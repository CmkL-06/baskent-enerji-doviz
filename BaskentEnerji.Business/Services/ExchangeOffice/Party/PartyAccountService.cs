using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Party;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Party;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Party;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOffice.Party;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Party
{
    public class PartyAccountService : IPartyAccountService
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly ILogger<PartyAccountService> _logger;
        private readonly ValidationService _validationService;
        private readonly IVaultService _vaultService;

        public PartyAccountService(BaskentEnerjiDbContext context, ILogger<PartyAccountService> logger, ValidationService validationService, IVaultService vaultService)
        {
            _context = context;
            _logger = logger;
            _validationService = validationService;
            _vaultService = vaultService;
        }

        public async Task<vm_partyaccount> CreateAccountAsync(Guid partyId, Guid currencyId)
        {
            var account = new PartyAccount
            {
                PartyId = partyId,
                CurrencyId = currencyId,
                AccountNumber = GenerateAccountNumber(),
                Balance = 0,
                BlockedAmount = 0,
                Status = AccountStatus.Active
            };

            _context.PartyAccounts.Add(account);
            await _context.SaveChangesAsync();

            return await GetAccountAsync(account.Id);
        }

        public async Task<vm_partyaccount> GetAccountAsync(Guid accountId)
        {
            var account = await _context.PartyAccounts
                .AsNoTracking()
                .Include(a => a.Party)
                .Include(a => a.Currency)
                .FirstOrDefaultAsync(a => a.Id == accountId);

            if (account == null)
                return null;

            return MapToViewModel(account);
        }

        public async Task<List<vm_partyaccount>> GetPartyAccountsAsync(Guid partyId)
        {
            var accounts = await _context.PartyAccounts
                .AsNoTracking()
                .Include(a => a.Currency)
                .Where(a => a.PartyId == partyId)
                .ToListAsync();

            return accounts.Select(MapToViewModel).ToList();
        }

        public async Task<vm_partyaccountentry> CreateManualEntryAsync(rm_partyaccountentry request)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var account = await _context.PartyAccounts
                    .Include(a => a.Party)
                    .Include(a => a.Currency)
                    .FirstOrDefaultAsync(a => a.Id == request.PartyAccountId);

                if (account == null)
                    throw new InvalidOperationException("Party account not found");

                if (account.Party != null)
                    await _validationService.EnsureNotViewerAsync(account.Party.OfficeId);

                var entry = new PartyAccountEntry
                {
                    PartyAccountId = request.PartyAccountId,
                    EntryNumber = GenerateEntryNumber(),
                    EntryDate = request.EntryDate.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(request.EntryDate, DateTimeKind.Local) : request.EntryDate,
                    Type = request.EntryType,
                    Amount = request.Amount,
                    Description = request.Description,
                    ReferenceNumber = request.ReferenceNumber,
                    DueDate = request.DueDate?.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(request.DueDate.Value, DateTimeKind.Local) : request.DueDate,
                    PaymentStatus = request.PaymentStatus,
                    CreatedByUserId = request.CreatedByUserId
                };

                _context.PartyAccountEntries.Add(entry);

                decimal vaultAmountChange = 0;
                if (request.EntryType == EntryType.Debit)
                {
                    account.Balance += request.Amount;
                    account.TotalDebits += request.Amount;
                    vaultAmountChange = -request.Amount;
                }
                else
                {
                    account.Balance -= request.Amount;
                    account.TotalCredits += request.Amount;
                    vaultAmountChange = request.Amount;
                }

                entry.RunningBalance = account.Balance;

                if (vaultAmountChange != 0 && account.Party != null)
                {
                    var activeVault = await _context.Vaults
                        .FirstOrDefaultAsync(v => v.OfficeId == account.Party.OfficeId && v.IsActive);

                    if (activeVault != null)
                    {
                        var vaultUpdate = new rm_updatevaultbalance
                        {
                            vaultId = activeVault.Id,
                            currencyId = account.CurrencyId,
                            amount = vaultAmountChange,
                            description = request.EntryType == EntryType.Debit
                                ? $"Cari hesap ödemesi: {account.Party.Name} - {request.Description ?? "Ödeme"}"
                                : $"Cari hesap tahsilatı: {account.Party.Name} - {request.Description ?? "Tahsilat"}",
                            isEntireBalance = false,
                            TransactionType = request.EntryType == EntryType.Debit ? TransactionType.Withdrawal : TransactionType.Deposit
                        };
                        await _vaultService.UpdateVaultBalanceAsync(vaultUpdate);
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return MapToViewModel(entry);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<vm_partyaccountentry> RecordPaymentAsync(rm_partypayment request)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
            var party = await _context.Parties.FindAsync(request.PartyId);
            if (party == null)
                throw new InvalidOperationException("Party not found");

            await _validationService.EnsureNotViewerAsync(party.OfficeId);

            var tryCurrency = await _context.Currencies.FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
            if (tryCurrency == null)
                throw new InvalidOperationException("TRY currency not found");

            var account = await _context.PartyAccounts
                .Include(a => a.Currency)
                .FirstOrDefaultAsync(a => a.PartyId == request.PartyId && a.CurrencyId == tryCurrency.Id);

            if (account == null)
            {
                account = new PartyAccount
                {
                    PartyId = request.PartyId,
                    CurrencyId = tryCurrency.Id,
                    AccountNumber = GenerateAccountNumber(),
                    Balance = 0,
                    BlockedAmount = 0,
                    Status = AccountStatus.Active,
                    Party = party
                };
                _context.PartyAccounts.Add(account);
                await _context.SaveChangesAsync();
            }

            // Amount'u pozitif yap (frontend negatif gönderebiliyor)
            var positiveAmount = Math.Abs(request.Amount);

            // TL karşılığını hesapla
            decimal tlAmount = positiveAmount;
            decimal exchangeRate = 1;
            string currencyCode = "TRY";

            // Eğer TL değilse kur hesapla
            if (request.CurrencyId != tryCurrency.Id)
            {
                var paymentCurrency = await _context.Currencies.FindAsync(request.CurrencyId);
                if (paymentCurrency == null)
                    throw new InvalidOperationException("Payment currency not found");

                currencyCode = paymentCurrency.CurrencyCode;

                // Özel kur veya market kuru kullan
                if (request.CustomExchangeRate.HasValue && request.CustomExchangeRate.Value > 0)
                {
                    exchangeRate = request.CustomExchangeRate.Value;
                }
                else
                {
                    // Office'in güncel kurunu al
                    var exchangeRateEntity = await _context.ExchangeRates
                        .Where(r => r.SourceCurrencyId == request.CurrencyId
                            && r.TargetCurrencyId == tryCurrency.Id
                            && r.OfficeId == request.OfficeId)
                        .OrderByDescending(r => r.CreatedDate)
                        .FirstOrDefaultAsync();

                    if (exchangeRateEntity == null)
                        throw new InvalidOperationException($"Exchange rate not found for {currencyCode} to TRY");

                    exchangeRate = exchangeRateEntity.SellRate;
                }

                tlAmount = positiveAmount * exchangeRate;
            }

            // Entry oluştur
            var entry = new PartyAccountEntry
            {
                PartyAccountId = account.Id,
                EntryNumber = GenerateEntryNumber(),
                EntryDate = request.PaymentDate.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(request.PaymentDate, DateTimeKind.Local)
                    : request.PaymentDate,
                Type = request.Type,
                Amount = tlAmount, // TL karşılığı
                Description = request.CurrencyId != tryCurrency.Id
                    ? $"{positiveAmount} {currencyCode} @ {exchangeRate:F4} = {tlAmount:F2} TRY - {request.Notes ?? request.PaymentMethod}"
                    : request.Notes ?? $"Payment - {request.PaymentMethod}",
                ReferenceNumber = request.PaymentReference,
                PaymentStatus = PaymentStatus.Paid,
                PaymentDate = request.PaymentDate.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(request.PaymentDate, DateTimeKind.Local)
                    : request.PaymentDate,
                PaymentReference = request.PaymentReference,
                CreatedByUserId = Guid.Parse(_validationService.GetUserID()),
                // Orijinal döviz bilgileri
                OriginalCurrencyId = request.CurrencyId,
                OriginalAmount = positiveAmount,
                ExchangeRate = exchangeRate,
                IsCustomRate = request.CustomExchangeRate.HasValue
            };

            _context.PartyAccountEntries.Add(entry);

            // Hesap bakiyesini güncelle (TL bazında)
            // Balance > 0 = Party bize borçlu
            // Balance < 0 = Biz party'ye borçluyuz

            if (request.Type == EntryType.Credit) // Tahsilat (party'den para alıyoruz)
            {
                account.Balance -= tlAmount; // Party'nin borcu azalır
                account.TotalCredits += tlAmount;
            }
            else if (request.Type == EntryType.Debit) // Ödeme (party'ye para veriyoruz)
            {
                account.Balance += tlAmount; // Party'nin borcu artar (veya alacağı azalır)
                account.TotalDebits += tlAmount;
            }

            account.LastActivityDate = DateTime.UtcNow;
            account.LastTransactionDate = DateTime.UtcNow;
            account.TransactionCount++;

            // Running balance'ı güncelle
            entry.RunningBalance = account.Balance;

            // Kasayı güncelle (gelen para birimi cinsinden)
            var activeVault = await _context.Vaults
                .FirstOrDefaultAsync(v => v.OfficeId == request.OfficeId && v.IsActive);

            if (activeVault != null)
            {
                var vaultUpdate = new rm_updatevaultbalance
                {
                    vaultId = activeVault.Id,
                    currencyId = request.CurrencyId, // Gelen para birimi
                    amount = request.Type == EntryType.Credit ? positiveAmount : -positiveAmount,
                    description = request.Type == EntryType.Credit
                        ? $"Cari tahsilat: {party.Name} - {positiveAmount} {currencyCode}"
                        : $"Cari ödeme: {party.Name} - {positiveAmount} {currencyCode}",
                    isEntireBalance = false,
                    TransactionType = TransactionType.Party
                };
                await _vaultService.UpdateVaultBalanceAsync(vaultUpdate);
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return MapToViewModel(entry);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<List<vm_partyaccountentry>> GetAccountEntriesAsync(Guid accountId, DateTime? fromDate = null, DateTime? toDate = null, PaymentStatus? status = null)
        {
            // SIMPLIFIED QUERY - removed all joins to test performance
            // If this is fast, the problem is with SQL Server query plan optimization
            var query = _context.PartyAccountEntries
                .AsNoTracking()
                .Where(e => e.PartyAccountId == accountId);

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;
                query = query.Where(e => e.EntryDate >= startDate);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(e => e.EntryDate <= endDate);
            }

            if (status.HasValue)
                query = query.Where(e => e.PaymentStatus == status.Value);

            // Get ONLY the entries WITHOUT any joins - let's see if this is fast
            var entries = await query
                .OrderBy(e => e.EntryDate)
                .ThenBy(e => e.CreatedDate)
                .ToListAsync();

            // If entries exist, do SEPARATE lookups for related data (much faster than joins)
            if (entries.Any())
            {
                // 1. Get accounts with party and currency info
                var accountIds = entries.Select(e => e.PartyAccountId).Distinct().ToList();
                var accounts = await _context.PartyAccounts
                    .AsNoTracking()
                    .Include(a => a.Party)
                    .Include(a => a.Currency)
                    .Where(a => accountIds.Contains(a.Id))
                    .ToListAsync();
                var accountLookup = accounts.ToDictionary(a => a.Id);

                // 2. Get transactions for entries that have TransactionId
                var transactionIds = entries.Where(e => e.TransactionId.HasValue).Select(e => e.TransactionId!.Value).Distinct().ToList();
                Dictionary<Guid, string> transactionLookup = new();
                if (transactionIds.Any())
                {
                    transactionLookup = await _context.Transactions
                        .AsNoTracking()
                        .Where(t => transactionIds.Contains(t.Id))
                        .ToDictionaryAsync(t => t.Id, t => t.TransactionNumber);
                }

                // 3. Get reconciled users for entries that are reconciled
                var userIds = entries.Where(e => e.ReconciledByUserId.HasValue).Select(e => e.ReconciledByUserId!.Value).Distinct().ToList();
                Dictionary<Guid, string> userLookup = new();
                if (userIds.Any())
                {
                    userLookup = await _context.Users
                        .AsNoTracking()
                        .Where(u => userIds.Contains(u.Id))
                        .ToDictionaryAsync(u => u.Id, u => $"{u.Firstname} {u.Lastname}".Trim());
                }

                // 4. Get original currencies for entries that have OriginalCurrencyId
                var currencyIds = entries.Where(e => e.OriginalCurrencyId.HasValue).Select(e => e.OriginalCurrencyId!.Value).Distinct().ToList();
                Dictionary<Guid, string> currencyLookup = new();
                if (currencyIds.Any())
                {
                    currencyLookup = await _context.Currencies
                        .AsNoTracking()
                        .Where(c => currencyIds.Contains(c.Id))
                        .ToDictionaryAsync(c => c.Id, c => c.CurrencyCode);
                }

                // Map to view models with in-memory lookups (super fast!)
                return entries.Select(e =>
                {
                    var account = accountLookup.GetValueOrDefault(e.PartyAccountId);
                    return new vm_partyaccountentry
                    {
                        Id = e.Id,
                        PartyAccountId = e.PartyAccountId,
                        AccountNumber = account?.AccountNumber,
                        PartyCode = account?.Party?.PartyCode,
                        PartyName = account?.Party?.Name,
                        CurrencyCode = account?.Currency?.CurrencyCode,
                        TransactionId = e.TransactionId,
                        TransactionNumber = e.TransactionId.HasValue ? transactionLookup.GetValueOrDefault(e.TransactionId.Value) : null,
                        EntryNumber = e.EntryNumber,
                        EntryDate = e.EntryDate,
                        DueDate = e.DueDate,
                        Type = e.Type,
                        TypeName = e.Type.ToString(),
                        Amount = e.Amount,
                        RunningBalance = e.RunningBalance,
                        Description = e.Description,
                        ReferenceNumber = e.ReferenceNumber,
                        PaymentStatus = e.PaymentStatus,
                        PaymentStatusName = e.PaymentStatus.ToString(),
                        PaymentDate = e.PaymentDate,
                        PaymentReference = e.PaymentReference,
                        IsReconciled = e.IsReconciled,
                        ReconciledDate = e.ReconciledDate,
                        ReconciledByUserName = e.ReconciledByUserId.HasValue ? userLookup.GetValueOrDefault(e.ReconciledByUserId.Value) : null,
                        CreatedDate = e.CreatedDate,
                        IsReversed = e.IsReversed,
                        ReversalEntryId = e.ReversalEntryId,
                        ReversalEntryNumber = null,
                        OriginalCurrencyId = e.OriginalCurrencyId,
                        OriginalCurrencyCode = e.OriginalCurrencyId.HasValue ? currencyLookup.GetValueOrDefault(e.OriginalCurrencyId.Value) : null,
                        OriginalAmount = e.OriginalAmount,
                        ExchangeRate = e.ExchangeRate,
                        IsCustomRate = e.IsCustomRate
                    };
                }).ToList();
            }

            return new List<vm_partyaccountentry>();
        }

        private vm_partyaccount MapToViewModel(PartyAccount account)
        {
            return new vm_partyaccount
            {
                Id = account.Id,
                PartyId = account.PartyId,
                PartyName = account.Party?.Name,
                CurrencyId = account.CurrencyId,
                CurrencyCode = account.Currency?.CurrencyCode,
                AccountNumber = account.AccountNumber,
                Balance = account.Balance,
                AvailableBalance = account.Balance - account.BlockedAmount,
                BlockedAmount = account.BlockedAmount,
                CreditLimit = account.CreditLimit,
                Status = account.Status,
                StatusName = account.Status.ToString(),
                TotalDebits = account.TotalDebits,
                TotalCredits = account.TotalCredits,
                LastActivityDate = account.LastActivityDate
            };
        }

        private vm_partyaccountentry MapToViewModel(PartyAccountEntry entry)
        {
            return new vm_partyaccountentry
            {
                Id = entry.Id,
                PartyAccountId = entry.PartyAccountId,
                AccountNumber = entry.PartyAccount?.AccountNumber,
                PartyCode = entry.PartyAccount?.Party?.PartyCode,
                PartyName = entry.PartyAccount?.Party?.Name,
                CurrencyCode = entry.PartyAccount?.Currency?.CurrencyCode,
                TransactionId = entry.TransactionId,
                TransactionNumber = entry.Transaction?.TransactionNumber,
                EntryNumber = entry.EntryNumber,
                EntryDate = entry.EntryDate,
                DueDate = entry.DueDate,
                Type = entry.Type,
                TypeName = entry.Type.ToString(),
                Amount = entry.Amount,
                RunningBalance = entry.RunningBalance,
                Description = entry.Description,
                ReferenceNumber = entry.ReferenceNumber,
                PaymentStatus = entry.PaymentStatus,
                PaymentStatusName = entry.PaymentStatus.ToString(),
                PaymentDate = entry.PaymentDate,
                PaymentReference = entry.PaymentReference,
                IsReconciled = entry.IsReconciled,
                ReconciledDate = entry.ReconciledDate,
                ReconciledByUserName = entry.ReconciledByUser != null ? $"{entry.ReconciledByUser.Firstname} {entry.ReconciledByUser.Lastname}".Trim() : null,
                CreatedDate = entry.CreatedDate,
                IsReversed = entry.IsReversed,
                ReversalEntryId = entry.ReversalEntryId,
                ReversalEntryNumber = null, // ReversalEntry navigation not loaded
                // Orijinal döviz bilgileri
                OriginalCurrencyId = entry.OriginalCurrencyId,
                OriginalCurrencyCode = entry.OriginalCurrency?.CurrencyCode,
                OriginalAmount = entry.OriginalAmount,
                ExchangeRate = entry.ExchangeRate,
                IsCustomRate = entry.IsCustomRate
            };
        }

        private string GenerateAccountNumber()
        {
            return $"PA{DateTime.UtcNow:yyyyMMdd}{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
        }

        private string GenerateEntryNumber()
        {
            return $"PE{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";
        }

        // Other interface methods would be implemented here...
        public Task<vm_partyaccount> GetAccountByPartyAndCurrencyAsync(Guid partyId, Guid currencyId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> BlockAmountAsync(Guid accountId, decimal amount, string reason)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UnblockAmountAsync(Guid accountId, decimal amount)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAccountStatusAsync(Guid accountId, AccountStatus status, string reason)
        {
            throw new NotImplementedException();
        }

        public Task<decimal> GetBalanceAsync(Guid partyId, Guid currencyId)
        {
            throw new NotImplementedException();
        }

        public Task<vm_partybalance> GetBalanceSummaryAsync(Guid partyId)
        {
            throw new NotImplementedException();
        }

        public Task<List<vm_partybalance>> GetAllBalancesAsync(Guid officeId, DateTime? asOfDate = null)
        {
            throw new NotImplementedException();
        }

        public Task<vm_partyaccountentry> CreateEntryAsync(rm_partyaccountentry request)
        {
            throw new NotImplementedException();
        }

        public Task<vm_partyaccountentry> ReverseEntryAsync(Guid entryId, string reason)
        {
            throw new NotImplementedException();
        }

        public Task<vm_partyaccountentry> GetEntryByReferenceAsync(string referenceNumber)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ReconcileEntryAsync(Guid entryId, Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UnreconcileEntryAsync(Guid entryId, string reason)
        {
            throw new NotImplementedException();
        }

        public Task<List<vm_partyaccountentry>> GetUnreconciledEntriesAsync(Guid accountId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdatePaymentStatusAsync(Guid entryId, PaymentStatus status, string paymentReference = null)
        {
            throw new NotImplementedException();
        }

        public Task<List<vm_partyaccountentry>> GetOverdueEntriesAsync(Guid? partyId = null, Guid? officeId = null)
        {
            throw new NotImplementedException();
        }

        private string GetTurkishPaymentMethod(string paymentMethod)
        {
            return paymentMethod?.ToLower() switch
            {
                "cash" => "Nakit",
                "transfer" => "Havale",
                "eft" => "EFT",
                "credit card" => "Kredi Kartı",
                "check" => "Çek",
                _ => paymentMethod ?? "Ödeme"
            };
        }
    }
}