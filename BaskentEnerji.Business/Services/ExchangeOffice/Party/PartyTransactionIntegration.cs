using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Party;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Party;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Party
{
    public class PartyTransactionIntegration
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly ILogger<PartyTransactionIntegration> _logger;
        private readonly IPartyCreditService _creditService;

        public PartyTransactionIntegration(BaskentEnerjiDbContext context, ILogger<PartyTransactionIntegration> logger, IPartyCreditService creditService)
        {
            _context = context;
            _logger = logger;
            _creditService = creditService;
        }

        /// <summary>
        /// Creates party account entries for a transaction
        /// </summary>
        public async Task CreatePartyAccountEntriesAsync(Transaction transaction)
        {
            try
            {
                if (!transaction.PartyId.HasValue)
                    return;

                var party = await _context.Parties
                    .Include(p => p.Accounts)
                    .FirstOrDefaultAsync(p => p.Id == transaction.PartyId.Value);

                if (party == null)
                {
                    _logger.LogWarning($"Party {transaction.PartyId} not found for transaction {transaction.TransactionNumber}");
                    return;
                }

                // Kredi limiti kontrolü — daha önce CheckCreditAvailabilityAsync hiçbir çağrı yolunda
                // kullanılmıyordu, bir müşteriye kredi limiti tanımlansa bile sistem sınırsız
                // borçlanmaya izin veriyordu. Asıl bakiye güncellemesinden ÖNCE, para birimi bazında
                // net borç (Debit) artışını kontrol ediyoruz — aşarsa işlem tamamen rollback olur
                // (bu metod ProcessExchangeAsync'in dış DB transaction'ı içinde çağrılıyor).
                var debitDeltasByCurrency = new Dictionary<Guid, decimal>();
                foreach (var d in transaction.Details)
                {
                    var (t, a) = DetermineEntryTypeAndAmount(transaction.Type, d);
                    if (t != EntryType.Debit || a == 0) continue;
                    debitDeltasByCurrency[d.CurrencyId] = debitDeltasByCurrency.GetValueOrDefault(d.CurrencyId) + a;
                }
                foreach (var (currencyId, deltaAmount) in debitDeltasByCurrency)
                {
                    var availability = await _creditService.CheckCreditAvailabilityAsync(party.Id, currencyId, deltaAmount);
                    if (availability.HasCreditLimit && !availability.IsApproved)
                        throw new ApiException(HttpStatusCode.BadRequest,
                            $"Cari hesap kredi limiti aşılıyor. Mevcut bakiye: {availability.CurrentBalance:F2}, " +
                            $"Limit: {availability.CreditLimit:F2}, İstenen: {deltaAmount:F2}. Sadece Patron/Admin limiti güncelleyebilir.");
                }

                // Process each transaction detail
                foreach (var detail in transaction.Details)
                {
                    // Get or create party account for the currency
                    var account = await GetOrCreatePartyAccountAsync(party.Id, detail.CurrencyId);

                    // Determine entry type based on transaction type and flow
                    var (entryType, amount) = DetermineEntryTypeAndAmount(transaction.Type, detail);

                    if (amount == 0)
                        continue;

                    // Create account entry
                    var entry = new PartyAccountEntry
                    {
                        PartyAccountId = account.Id,
                        TransactionId = transaction.Id,
                        EntryNumber = GenerateEntryNumber(),
                        Type = entryType,
                        Amount = Math.Abs(amount),
                        Description = GenerateEntryDescription(transaction, detail),
                        ReferenceNumber = transaction.TransactionNumber,
                        EntryDate = transaction.TransactionDate,
                        DueDate = CalculateDueDate(party, transaction.TransactionDate),
                        PaymentStatus = PaymentStatus.Pending,
                        CreatedByUserId = transaction.UserId,
                        IsReconciled = false
                    };

                    // Calculate running balance
                    var previousBalance = await _context.PartyAccountEntries
                        .Where(e => e.PartyAccountId == account.Id && e.EntryDate <= entry.EntryDate)
                        .OrderByDescending(e => e.EntryDate)
                        .ThenByDescending(e => e.CreatedDate)
                        .Select(e => e.RunningBalance)
                        .FirstOrDefaultAsync();

                    entry.RunningBalance = CalculateNewBalance(previousBalance, entry.Type, entry.Amount);

                    _context.PartyAccountEntries.Add(entry);

                    // Update account balance
                    await UpdateAccountBalanceAsync(account, entry.Type, entry.Amount);
                }

                // Update party last transaction date
                party.LastTransactionDate = transaction.TransactionDate;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created party account entries for transaction {transaction.TransactionNumber}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating party account entries for transaction {transaction.TransactionNumber}");
                throw;
            }
        }

        /// <summary>
        /// Reverses party account entries for a cancelled transaction
        /// </summary>
        public async Task ReversePartyAccountEntriesAsync(Guid transactionId, string reason)
        {
            try
            {
                var entries = await _context.PartyAccountEntries
                    .Include(e => e.PartyAccount)
                    .Where(e => e.TransactionId == transactionId && !e.IsReversed)
                    .ToListAsync();

                foreach (var entry in entries)
                {
                    // Create reversal entry
                    var reversalEntry = new PartyAccountEntry
                    {
                        PartyAccountId = entry.PartyAccountId,
                        TransactionId = entry.TransactionId,
                        EntryNumber = GenerateEntryNumber(),
                        Type = entry.Type == EntryType.Debit ? EntryType.Credit : EntryType.Debit,
                        Amount = entry.Amount,
                        Description = $"Reversal: {entry.Description} - {reason}",
                        ReferenceNumber = $"REV-{entry.ReferenceNumber}",
                        EntryDate = DateTime.UtcNow,
                        PaymentStatus = PaymentStatus.Paid,
                        CreatedByUserId = entry.CreatedByUserId,
                        IsReconciled = true,
                        ReconciledDate = DateTime.UtcNow
                    };

                    // Calculate running balance — CreatePartyAccountEntriesAsync'teki ile AYNI sıralama
                    // kullanılmalı (EntryDate önce, sonra CreatedDate); sadece CreatedDate'e göre
                    // sıralamak, geriye dönük tarihli (EntryDate geçmişte olan) kayıtlar varken
                    // yanlış "önceki bakiye"yi seçip RunningBalance'ı o noktadan sonra tutarsız bırakırdı.
                    var previousBalance = await _context.PartyAccountEntries
                        .Where(e => e.PartyAccountId == entry.PartyAccountId)
                        .OrderByDescending(e => e.EntryDate)
                        .ThenByDescending(e => e.CreatedDate)
                        .Select(e => e.RunningBalance)
                        .FirstOrDefaultAsync();

                    reversalEntry.RunningBalance = CalculateNewBalance(previousBalance, reversalEntry.Type, reversalEntry.Amount);

                    _context.PartyAccountEntries.Add(reversalEntry);

                    // Mark original entry as reversed
                    entry.IsReversed = true;
                    entry.ReversalEntryId = reversalEntry.Id;

                    // Update account balance
                    await UpdateAccountBalanceAsync(entry.PartyAccount, reversalEntry.Type, reversalEntry.Amount);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Reversed party account entries for transaction {transactionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reversing party account entries for transaction {transactionId}");
                throw;
            }
        }

        private async Task<PartyAccount> GetOrCreatePartyAccountAsync(Guid partyId, Guid currencyId)
        {
            var account = await _context.PartyAccounts
                .FirstOrDefaultAsync(a => a.PartyId == partyId && a.CurrencyId == currencyId);

            if (account == null)
            {
                account = new PartyAccount
                {
                    PartyId = partyId,
                    CurrencyId = currencyId,
                    AccountNumber = GenerateAccountNumber(partyId, currencyId),
                    Balance = 0,
                    BlockedAmount = 0,
                    TotalDebits = 0,
                    TotalCredits = 0,
                    TransactionCount = 0,
                    Status = AccountStatus.Active
                };

                _context.PartyAccounts.Add(account);
                await _context.SaveChangesAsync();
            }

            return account;
        }

        private (EntryType type, decimal amount) DetermineEntryTypeAndAmount(TransactionType transactionType, TransactionDetail detail)
        {
            // This logic depends on your business rules
            // For exchange transactions:
            // - If party is buying currency (we're selling), it's a receivable (Debit)
            // - If party is selling currency (we're buying), it's a payable (Credit)
            
            switch (transactionType)
            {
                case TransactionType.Exchange:
                case TransactionType.Buy:
                    // Credit detail = office receives currency from customer → party Credit (we owe them)
                    // Debit detail = office pays currency to customer → party Debit (they owe us)
                    return detail.Side == TransactionSide.Credit
                        ? (EntryType.Credit, detail.NetAmount)
                        : (EntryType.Debit, detail.NetAmount);
                
                case TransactionType.Deposit:
                    // Deposits reduce party payables or create receivables
                    return (EntryType.Credit, detail.Amount);
                
                case TransactionType.Withdrawal:
                    // Withdrawals reduce party receivables or create payables
                    return (EntryType.Debit, detail.Amount);
                
                default:
                    return (EntryType.Debit, 0);
            }
        }

        private string GenerateEntryDescription(Transaction transaction, TransactionDetail detail)
        {
            return $"{transaction.Type} - {detail.Currency?.CurrencyCode ?? "N/A"} {detail.Amount:N2}";
        }

        private DateTime? CalculateDueDate(Entity.Entities.ExchangeOffice.Party.Party party, DateTime transactionDate)
        {
            if (party.DefaultPaymentTermDays > 0)
            {
                return transactionDate.AddDays(party.DefaultPaymentTermDays);
            }
            return null;
        }

        private decimal CalculateNewBalance(decimal previousBalance, EntryType entryType, decimal amount)
        {
            return entryType == EntryType.Debit 
                ? previousBalance + amount 
                : previousBalance - amount;
        }

        private async Task UpdateAccountBalanceAsync(PartyAccount account, EntryType entryType, decimal amount)
        {
            if (entryType == EntryType.Debit)
            {
                account.Balance += amount;
                account.TotalDebits += amount;
            }
            else
            {
                account.Balance -= amount;
                account.TotalCredits += amount;
            }

            account.TransactionCount++;
            account.LastTransactionDate = DateTime.UtcNow;
        }

        private string GenerateEntryNumber()
        {
            return $"PE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        }

        private string GenerateAccountNumber(Guid partyId, Guid currencyId)
        {
            var timestamp = DateTime.UtcNow.ToString("yyMMdd");
            var partyShort = partyId.ToString("N").Substring(0, 4).ToUpper();
            var currencyShort = currencyId.ToString("N").Substring(0, 4).ToUpper();
            return $"PA-{timestamp}-{partyShort}-{currencyShort}";
        }
    }
}