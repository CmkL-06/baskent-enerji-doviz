using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Office
{
    public class ExchangeReportingService : IExchangeReportingService
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly IVaultService _vaultService;

        public ExchangeReportingService(BaskentEnerjiDbContext context, IVaultService vaultService)
        {
            _context = context;
            _vaultService = vaultService;
        }

        public async Task<vm_monthlyreport> GetMonthlyReport(Guid officeId, int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddSeconds(-1);

            // Get office info
            var office = await _context.Offices
                .FirstOrDefaultAsync(o => o.Id == officeId);

            if (office == null)
                throw new InvalidOperationException("Office not found");

            // Get all transactions for the month
            var transactions = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Details)
                    .ThenInclude(d => d.Currency)
                .Include(t => t.Vault)
                .Where(t => t.Vault.OfficeId == officeId &&
                           t.TransactionDate >= startDate &&
                           t.TransactionDate <= endDate &&
                           t.Status == TransactionStatus.Completed)
                .AsSplitQuery()
                .ToListAsync();

            // Calculate volumes by currency and track total profit
            var volumesByCurrency = new Dictionary<string, decimal>();
            var profitsByCurrency = new Dictionary<string, decimal>();
            var totalProfit = 0m;
            var totalDeposits = 0m;
            var totalWithdrawals = 0m;

            foreach (var transaction in transactions)
            {
                if (transaction.Type == TransactionType.Exchange || transaction.Type == TransactionType.Buy)
                {
                    // Add to total profit
                    totalProfit += transaction.Profit;

                    // Count debit details to distribute profit evenly across currencies
                    var debitDetails = transaction.Details.Where(d => d.Side == TransactionSide.Debit).ToList();
                    var profitPerDebit = debitDetails.Count > 0 ? transaction.Profit / debitDetails.Count : 0;

                    foreach (var detail in transaction.Details)
                    {
                        var currencyCode = detail.Currency.CurrencyCode;

                        if (!volumesByCurrency.ContainsKey(currencyCode))
                        {
                            volumesByCurrency[currencyCode] = 0;
                            profitsByCurrency[currencyCode] = 0;
                        }

                        volumesByCurrency[currencyCode] += Math.Abs(detail.Amount);

                        if (detail.Side == TransactionSide.Debit)
                        {
                            profitsByCurrency[currencyCode] += profitPerDebit;
                        }
                    }
                }
                else if (transaction.Type == TransactionType.Deposit)
                {
                    totalDeposits += transaction.Details.Sum(d => Math.Abs(d.Amount * d.Rate));
                }
                else if (transaction.Type == TransactionType.Withdrawal)
                {
                    totalWithdrawals += transaction.Details.Sum(d => Math.Abs(d.Amount * d.Rate));
                }
            }

            // Calculate daily performance
            var dailyPerformance = new List<vm_dailyperformance>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dayTransactions = transactions
                    .Where(t => t.TransactionDate.Date == date.Date)
                    .ToList();

                var exchangeTransactions = dayTransactions.Where(t => t.Type == TransactionType.Exchange || t.Type == TransactionType.Buy).ToList();
                var depositTransactions = dayTransactions.Where(t => t.Type == TransactionType.Deposit).ToList();
                var withdrawalTransactions = dayTransactions.Where(t => t.Type == TransactionType.Withdrawal).ToList();

                var dayVolume = exchangeTransactions
                    .SelectMany(t => t.Details)
                    .Sum(d => Math.Abs(d.Amount * d.Rate)); // Convert to base currency

                var dayProfit = exchangeTransactions.Sum(t => t.Profit);

                dailyPerformance.Add(new vm_dailyperformance
                {
                    Date = date,
                    TransactionCount = dayTransactions.Count,
                    ExchangeCount = exchangeTransactions.Count,
                    DepositCount = depositTransactions.Count,
                    WithdrawalCount = withdrawalTransactions.Count,
                    Volume = dayVolume,
                    Profit = dayProfit,
                    ProfitMargin = dayVolume > 0 ? (dayProfit / dayVolume) * 100 : 0,
                    AverageTransactionSize = exchangeTransactions.Count > 0 ? dayVolume / exchangeTransactions.Count : 0
                });
            }

            // Calculate total volume in base currency
            var totalVolume = 0m;
            foreach (var kvp in volumesByCurrency)
            {
                var currency = await _context.Currencies
                    .FirstOrDefaultAsync(c => c.CurrencyCode == kvp.Key);

                if (currency != null)
                {
                    var rate = await GetExchangeRateToBase(officeId, currency.Id);
                    totalVolume += kvp.Value * rate;
                }
            }

            // Get currency performance for the month
            var currencyPerformance = await GetCurrencyPerformance(officeId, startDate, endDate);

            // Calculate statistics
            var statistics = new vm_monthlystatistics();
            if (dailyPerformance.Any(d => d.Profit != 0))
            {
                var profitDays = dailyPerformance.Where(d => d.Profit > 0).ToList();
                var lossDays = dailyPerformance.Where(d => d.Profit < 0).ToList();
                
                statistics.BestDayProfit = dailyPerformance.Max(d => d.Profit);
                statistics.BestDayDate = dailyPerformance.First(d => d.Profit == statistics.BestDayProfit).Date;
                statistics.WorstDayProfit = dailyPerformance.Min(d => d.Profit);
                statistics.WorstDayDate = dailyPerformance.First(d => d.Profit == statistics.WorstDayProfit).Date;
                statistics.AverageDailyVolume = dailyPerformance.Average(d => d.Volume);
                statistics.AverageDailyProfit = dailyPerformance.Average(d => d.Profit);
                statistics.DaysWithProfit = profitDays.Count;
                statistics.DaysWithLoss = lossDays.Count;
                statistics.TotalExchangeTransactions = transactions.Count(t => t.Type == TransactionType.Exchange || t.Type == TransactionType.Buy);
                statistics.TotalDeposits = totalDeposits;
                statistics.TotalWithdrawals = totalWithdrawals;
                
                if (volumesByCurrency.Any())
                {
                    statistics.MostTradedCurrency = volumesByCurrency.OrderByDescending(kvp => kvp.Value).First().Key;
                }
                
                if (profitsByCurrency.Any())
                {
                    statistics.MostProfitableCurrency = profitsByCurrency.OrderByDescending(kvp => kvp.Value).First().Key;
                }
            }

            return new vm_monthlyreport
            {
                OfficeId = officeId,
                OfficeName = office.OfficeName,
                Year = year,
                Month = month,
                TotalTransactions = transactions.Count,
                TotalVolume = totalVolume,
                TotalProfit = totalProfit,
                AverageProfit = transactions.Count > 0 ? totalProfit / transactions.Count : 0,
                ProfitMargin = totalVolume > 0 ? (totalProfit / totalVolume) * 100 : 0,
                VolumesByCurrency = volumesByCurrency,
                ProfitsByCurrency = profitsByCurrency,
                DailyPerformance = dailyPerformance,
                CurrencyPerformance = currencyPerformance,
                Statistics = statistics
            };
        }

        public async Task<List<vm_currencyperformance>> GetCurrencyPerformance(
            Guid officeId,
            DateTime startDate,
            DateTime endDate)
        {
            var transactions = await _context.Transactions
                .Include(t => t.Details)
                    .ThenInclude(d => d.Currency)
                .Include(t => t.Vault)
                .Where(t => t.Vault.OfficeId == officeId &&
                           t.TransactionDate >= startDate &&
                           t.TransactionDate <= endDate &&
                           t.Status == TransactionStatus.Completed &&
                           (t.Type == TransactionType.Exchange || t.Type == TransactionType.Buy))
                .ToListAsync();

            var currencyPerformance = new Dictionary<Guid, vm_currencyperformance>();

            // Track spread observation counts for correct averaging
            var spreadCounts = new Dictionary<Guid, int>();

            foreach (var transaction in transactions)
            {
                var debitDetails = transaction.Details.Where(d => d.Side == TransactionSide.Debit).ToList();
                var profitPerDebit = debitDetails.Count > 0 ? transaction.Profit / debitDetails.Count : 0;

                foreach (var detail in transaction.Details)
                {
                    if (!currencyPerformance.ContainsKey(detail.CurrencyId))
                    {
                        currencyPerformance[detail.CurrencyId] = new vm_currencyperformance
                        {
                            CurrencyCode = detail.Currency.CurrencyCode,
                            CurrencyName = detail.Currency.CurrencyName,
                            TotalVolume = 0,
                            TotalProfit = 0,
                            TransactionCount = 0,
                            AverageSpread = 0
                        };
                        spreadCounts[detail.CurrencyId] = 0;
                    }

                    var perf = currencyPerformance[detail.CurrencyId];
                    perf.TotalVolume += Math.Abs(detail.Amount);

                    if (detail.Side == TransactionSide.Debit)
                    {
                        perf.TotalProfit += profitPerDebit;
                    }
                    perf.TransactionCount++;

                    if (detail.Side == TransactionSide.Credit && detail.Rate > 0)
                    {
                        var exchangeRate = await GetCurrentExchangeRate(officeId, detail.CurrencyId);
                        if (exchangeRate != null)
                        {
                            var spread = Math.Abs(exchangeRate.SellRate - exchangeRate.BuyRate);
                            spreadCounts[detail.CurrencyId]++;
                            var n = spreadCounts[detail.CurrencyId];
                            perf.AverageSpread += (spread - perf.AverageSpread) / n;
                        }
                    }
                }
            }

            return currencyPerformance.Values
                .OrderByDescending(p => p.TotalProfit)
                .ToList();
        }

        public async Task<List<vm_vaultutilization>> GetVaultUtilization(Guid? officeId = null)
        {
            var query = _context.Vaults
                .Include(v => v.Office)
                .Include(v => v.Balances)
                    .ThenInclude(b => b.Currency)
                .Where(v => v.IsActive);

            if (officeId.HasValue)
            {
                query = query.Where(v => v.OfficeId == officeId.Value);
            }

            var vaults = await query.ToListAsync();
            var utilizationReports = new List<vm_vaultutilization>();

            foreach (var vault in vaults)
            {
                var currentBalances = new Dictionary<string, decimal>();
                var averageBalances = new Dictionary<string, decimal>();
                var utilizationRates = new Dictionary<string, decimal>();
                var totalValue = 0m;

                // Get current balances
                foreach (var balance in vault.Balances)
                {
                    currentBalances[balance.Currency.CurrencyCode] = balance.Balance;

                    // Convert to base currency
                    var rate = await GetExchangeRateToBase(vault.OfficeId, balance.CurrencyId);
                    totalValue += balance.Balance * rate;

                    // Calculate 30-day average balance
                    var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                    var dailySummaries = await _context.DailySummaries
                        .Where(ds => ds.VaultId == vault.Id &&
                                    ds.CurrencyId == balance.CurrencyId &&
                                    ds.SummaryDate >= thirtyDaysAgo)
                        .ToListAsync();

                    if (dailySummaries.Any())
                    {
                        var avgBalance = dailySummaries.Average(ds => ds.ClosingBalance);
                        averageBalances[balance.Currency.CurrencyCode] = avgBalance;

                        // Calculate utilization rate (how much of average balance is being used)
                        // Assuming optimal utilization is 80% of average
                        var optimalBalance = avgBalance * 0.8m;
                        utilizationRates[balance.Currency.CurrencyCode] = optimalBalance > 0
                            ? (balance.Balance / optimalBalance) * 100
                            : 0;
                    }
                    else
                    {
                        averageBalances[balance.Currency.CurrencyCode] = balance.Balance;
                        utilizationRates[balance.Currency.CurrencyCode] = 100;
                    }
                }

                utilizationReports.Add(new vm_vaultutilization
                {
                    VaultId = vault.Id,
                    VaultName = vault.Name,
                    OfficeName = vault.Office.OfficeName,
                    CurrentBalances = currentBalances,
                    AverageBalances = averageBalances,
                    UtilizationRates = utilizationRates,
                    TotalValueInBaseCurrency = totalValue
                });
            }

            return utilizationReports
                .OrderByDescending(u => u.TotalValueInBaseCurrency)
                .ToList();
        }

        private async Task<decimal> GetExchangeRateToBase(Guid officeId, Guid currencyId)
        {
         
            var baseCurrency = await _context.Currencies
                .FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");

            if (baseCurrency == null || currencyId == baseCurrency.Id)
                return 1m;

            var rate = await _context.ExchangeRates
                .Where(r => r.OfficeId == officeId &&
                           r.SourceCurrencyId == currencyId &&
                           r.TargetCurrencyId == baseCurrency.Id &&
                           r.IsActive)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync();

            return rate?.BuyRate ?? 0m;
        }

        private async Task<ExchangeRate> GetCurrentExchangeRate(Guid officeId, Guid currencyId)
        {
            // Get rate to base currency for comparison
            var baseCurrency = await _context.Currencies
                .FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");

            if (baseCurrency == null)
                return null;

            return await _context.ExchangeRates
                .Where(r => r.OfficeId == officeId &&
                           r.SourceCurrencyId == currencyId &&
                           r.TargetCurrencyId == baseCurrency.Id &&
                           r.IsActive)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync();
        }
    }
}