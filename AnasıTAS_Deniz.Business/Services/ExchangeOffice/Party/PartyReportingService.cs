using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AnasıTAS_Deniz.Business.Infrastructure.ExchangeOffice.Party;
using AnasıTAS_Deniz.Data.Contexts;
using AnasıTAS_Deniz.Entity.Modals.ViewModals.ExchangeOffice.Party;
using AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Party;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Services.ExchangeOffice.Party
{
    public class PartyReportingService : IPartyReportingService
    {
        private readonly AnasıTAS_DenizDbContext _context;
        private readonly ILogger<PartyReportingService> _logger;

        public PartyReportingService(AnasıTAS_DenizDbContext context, ILogger<PartyReportingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<vm_partystatement> GenerateStatementAsync(Guid partyId, Guid currencyId, DateTime fromDate, DateTime toDate)
        {
            var party = await _context.Parties
                .FirstOrDefaultAsync(p => p.Id == partyId);

            if (party == null)
                throw new InvalidOperationException("Party not found");

            var currency = await _context.Currencies
                .FirstOrDefaultAsync(c => c.Id == currencyId);

            // Ensure we include the entire end day
            var endDate = toDate.Date.AddDays(1).AddSeconds(-1);

            var entries = await _context.PartyAccountEntries
                .Include(e => e.PartyAccount)
                    .ThenInclude(a => a.Currency)
                .Include(e => e.Transaction)
                .Include(e => e.CreatedByUser)
                .Where(e => e.PartyAccount.PartyId == partyId && 
                           e.PartyAccount.CurrencyId == currencyId &&
                           e.EntryDate >= fromDate.Date && 
                           e.EntryDate <= endDate)
                .OrderBy(e => e.EntryDate)
                .ThenBy(e => e.CreatedDate)
                .ToListAsync();

            var openingBalance = await GetOpeningBalanceAsync(partyId, currencyId, fromDate);

            // Calculate running balances
            decimal runningBalance = openingBalance;
            var statementEntries = entries.Select(e => 
            {
                if (e.Type == EntryType.Debit)
                    runningBalance += e.Amount;
                else
                    runningBalance -= e.Amount;

                return new vm_partyaccountentry
                {
                    Id = e.Id,
                    PartyAccountId = e.PartyAccountId,
                    AccountNumber = e.PartyAccount?.AccountNumber,
                    PartyCode = party.PartyCode,
                    PartyName = party.Name,
                    CurrencyCode = e.PartyAccount?.Currency?.CurrencyCode,
                    TransactionId = e.TransactionId,
                    TransactionNumber = e.Transaction?.TransactionNumber,
                    EntryNumber = e.EntryNumber,
                    EntryDate = e.EntryDate,
                    DueDate = e.DueDate,
                    Type = e.Type,
                    TypeName = e.Type.ToString(),
                    Amount = e.Amount,
                    RunningBalance = runningBalance,
                    Description = e.Description,
                    ReferenceNumber = e.ReferenceNumber,
                    PaymentStatus = e.PaymentStatus,
                    PaymentStatusName = e.PaymentStatus.ToString(),
                    PaymentDate = e.PaymentDate,
                    PaymentReference = e.PaymentReference,
                    IsReconciled = e.IsReconciled,
                    ReconciledDate = e.ReconciledDate,
                    CreatedDate = e.CreatedDate
                };
            }).ToList();

            var statement = new vm_partystatement
            {
                PartyId = partyId,
                PartyCode = party.PartyCode,
                PartyName = party.Name,
                PartyAddress = party.Address,
                PartyEmail = party.Email,
                PartyPhone = party.Phone,
                PartyTaxNumber = party.TaxNumber,
                CurrencyId = currencyId,
                CurrencyCode = currency?.CurrencyCode,
                CurrencyName = currency?.CurrencyName,
                StatementDate = DateTime.Now,
                PeriodStart = fromDate,
                PeriodEnd = toDate,
                OpeningBalance = openingBalance,
                Entries = statementEntries,
                GeneratedDate = DateTime.Now
            };

            statement.TotalDebits = entries.Where(e => e.Type == EntryType.Debit).Sum(e => e.Amount);
            statement.TotalCredits = entries.Where(e => e.Type == EntryType.Credit).Sum(e => Math.Abs(e.Amount));
            statement.ClosingBalance = openingBalance + statement.TotalDebits - statement.TotalCredits;

            return statement;
        }

        public async Task<List<vm_agedreceivables>> GetAgedReceivablesAsync(Guid officeId, DateTime asOfDate)
        {
            var parties = await _context.Parties
                .Include(p => p.Accounts)
                    .ThenInclude(a => a.Currency)
                .Where(p => p.OfficeId == officeId && p.IsActive)
                .ToListAsync();

            var result = new List<vm_agedreceivables>();

            foreach (var party in parties)
            {
                foreach (var account in party.Accounts.Where(a => a.Balance > 0))
                {
                    var overdueEntries = await _context.PartyAccountEntries
                        .Where(e => e.PartyAccountId == account.Id &&
                                   e.PaymentStatus == Entity.Entities.ExchangeOffice.Party.PaymentStatus.Pending &&
                                   e.Type == Entity.Entities.ExchangeOffice.Party.EntryType.Debit &&
                                   e.EntryDate <= asOfDate)
                        .ToListAsync();

                    if (!overdueEntries.Any())
                        continue;

                    var aged = new vm_agedreceivables
                    {
                        PartyId = party.Id,
                        PartyCode = party.PartyCode,
                        PartyName = party.Name,
                        CurrencyId = account.CurrencyId,
                        CurrencyCode = account.Currency.CurrencyCode,
                        TotalOutstanding = overdueEntries.Sum(e => e.Amount),
                        Details = new List<vm_agedinvoice>()
                    };

                    foreach (var entry in overdueEntries)
                    {
                        var daysOverdue = (asOfDate - (entry.DueDate ?? entry.EntryDate)).Days;
                        
                        if (daysOverdue <= 0)
                            aged.Current += entry.Amount;
                        else if (daysOverdue <= 30)
                            aged.Days1To30 += entry.Amount;
                        else if (daysOverdue <= 60)
                            aged.Days31To60 += entry.Amount;
                        else if (daysOverdue <= 90)
                            aged.Days61To90 += entry.Amount;
                        else
                            aged.Over90Days += entry.Amount;

                        aged.Details.Add(new vm_agedinvoice
                        {
                            EntryId = entry.Id,
                            ReferenceNumber = entry.ReferenceNumber,
                            EntryDate = entry.EntryDate,
                            DueDate = entry.DueDate,
                            Amount = entry.Amount,
                            OutstandingAmount = entry.Amount,
                            DaysOverdue = daysOverdue > 0 ? daysOverdue : 0,
                            Description = entry.Description
                        });
                    }

                    aged.OldestInvoiceDate = overdueEntries.Min(e => e.EntryDate);
                    aged.DaysOutstanding = (asOfDate - aged.OldestInvoiceDate).Days;

                    result.Add(aged);
                }
            }

            return result;
        }

        public async Task<List<vm_partybalance>> GetPartyBalanceSummaryAsync(Guid officeId)
        {
            var parties = await _context.Parties
                .Include(p => p.Accounts)
                    .ThenInclude(a => a.Currency)
                .Where(p => p.OfficeId == officeId && p.IsActive)
                .ToListAsync();

            var result = new List<vm_partybalance>();

            foreach (var party in parties)
            {
                var balances = new Dictionary<string, decimal>();
                decimal totalReceivables = 0;
                decimal totalPayables = 0;

                foreach (var account in party.Accounts)
                {
                    balances[account.Currency.CurrencyCode] = account.Balance;
                    
                    if (account.Balance > 0)
                        totalReceivables += account.Balance;
                    else
                        totalPayables += Math.Abs(account.Balance);
                }

                result.Add(new vm_partybalance
                {
                    PartyId = party.Id,
                    PartyCode = party.PartyCode,
                    PartyName = party.Name,
                    PartyType = party.Type.ToString(),
                    TotalReceivables = totalReceivables,
                    TotalPayables = totalPayables,
                    NetBalance = totalReceivables - totalPayables,
                    BalancesByCurrency = balances,
                    LastTransactionDate = party.LastTransactionDate,
                    AsOfDate = DateTime.Now
                });
            }

            return result.OrderByDescending(p => p.NetBalance).ToList();
        }

        private async Task<decimal> GetOpeningBalanceAsync(Guid partyId, Guid currencyId, DateTime beforeDate)
        {
            var entries = await _context.PartyAccountEntries
                .Include(e => e.PartyAccount)
                .Where(e => e.PartyAccount.PartyId == partyId && 
                           e.PartyAccount.CurrencyId == currencyId &&
                           e.EntryDate < beforeDate)
                .OrderBy(e => e.EntryDate)
                .ToListAsync();

            decimal balance = 0;
            foreach (var entry in entries)
            {
                if (entry.Type == Entity.Entities.ExchangeOffice.Party.EntryType.Debit)
                    balance += entry.Amount;
                else
                    balance -= entry.Amount;
            }

            return balance;
        }

        // Other interface methods would be implemented here...
        public Task<vm_partystatement> GetStatementAsync(Guid statementId)
        {
            throw new NotImplementedException();
        }

        public Task<List<vm_partystatement>> GetStatementsAsync(Guid partyId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> MarkStatementAsSentAsync(Guid statementId, string sentTo)
        {
            throw new NotImplementedException();
        }

        public Task<vm_agingreport> GetAgingReportAsync(Guid? partyId = null, Guid? officeId = null, DateTime? asOfDate = null)
        {
            throw new NotImplementedException();
        }

        public Task<vm_agingsummary> GetAgingSummaryAsync(Guid officeId, DateTime? asOfDate = null)
        {
            throw new NotImplementedException();
        }

        public Task<List<vm_partyaging>> GetPartiesWithOverdueBalancesAsync(Guid officeId, int daysOverdue)
        {
            throw new NotImplementedException();
        }

        public Task<vm_balancereport> GetBalanceReportAsync(Guid officeId, DateTime? asOfDate = null)
        {
            throw new NotImplementedException();
        }

        public Task<vm_reconciliationreport> GetReconciliationReportAsync(Guid partyId, Guid currencyId, DateTime fromDate, DateTime toDate)
        {
            throw new NotImplementedException();
        }

        public Task<List<vm_topparties>> GetTopPartiesByBalanceAsync(Guid officeId, int topCount = 10, bool receivables = true)
        {
            throw new NotImplementedException();
        }

        public Task<vm_transactionreport> GetTransactionReportAsync(Guid partyId, DateTime fromDate, DateTime toDate)
        {
            throw new NotImplementedException();
        }

        public Task<vm_partysummary> GetPartySummaryAsync(Guid partyId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            throw new NotImplementedException();
        }

        public Task<List<vm_dailyactivity>> GetDailyActivityReportAsync(Guid officeId, DateTime date)
        {
            throw new NotImplementedException();
        }

        public Task<vm_partyperformance> GetPartyPerformanceAsync(Guid partyId, DateTime fromDate, DateTime toDate)
        {
            throw new NotImplementedException();
        }

        public Task<vm_creditperformance> GetCreditPerformanceReportAsync(Guid officeId, DateTime fromDate, DateTime toDate)
        {
            throw new NotImplementedException();
        }

        public Task<List<vm_overduereport>> GetOverdueReportAsync(Guid officeId)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ExportStatementToPdfAsync(Guid statementId)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ExportAgingReportToExcelAsync(Guid officeId, DateTime asOfDate)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ExportBalanceReportToExcelAsync(Guid officeId, DateTime asOfDate)
        {
            throw new NotImplementedException();
        }
    }
}