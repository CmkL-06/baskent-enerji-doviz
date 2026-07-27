using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Office
{
    public class ZReportService : IZReportService
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly IVaultService _vaultService;
        private readonly IWacService _wacService;
        private readonly ValidationService _validationService;
        private readonly IMemoryCache _memoryCache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(3);
        private static readonly TimeZoneInfo TurkeyTz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

        public ZReportService(BaskentEnerjiDbContext context, IVaultService vaultService, IWacService wacService, ValidationService validationService, IMemoryCache memoryCache)
        {
            _context = context;
            _vaultService = vaultService;
            _wacService = wacService;
            _validationService = validationService;
            _memoryCache = memoryCache;
        }

        public async Task<vm_zreport> GetDailyZReport(Guid? officeId, DateTime date)
        {
            // DayClosureService "iş günü"nü Türkiye yerel saatine göre belirliyor (TurkeyTz.Date).
            // Burada da aynı yerel takvim gününün sınırları kullanılıyor, ama işlemler DB'de UTC
            // olarak kaydedildiği için (TransactionDate = DateTime.UtcNow) sorgu filtresine geçmeden
            // önce bu yerel gün sınırları UTC'ye çevrilmeli — aksi halde TRT 00:00-03:00 arası
            // işlemler (henüz UTC gece yarısı geçmemişken) bir önceki takvim gününe düşer ve
            // gün kapanışının "bugün" saydığı işlemlerle Z-Raporu'nun gösterdiği işlemler ayrışır.
            var localDayStart = date.Date;
            var utcStart = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDayStart, DateTimeKind.Unspecified), TurkeyTz);
            var utcEnd = utcStart.AddDays(1).AddTicks(-1);

            var startDate = utcStart;
            var endDate = utcEnd;

            // Check cache first
            var cacheKey = $"ZReport_Daily_{officeId}_{localDayStart:yyyyMMdd}";
            if (_memoryCache.TryGetValue(cacheKey, out vm_zreport cachedReport))
            {
                return cachedReport;
            }

            var report = await GenerateZReport(officeId, startDate, endDate, ZReportPeriod.Daily);
            
            // Cache the result if it's not today (today's data changes)
            if (date.Date < TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTz).Date)
            {
                _memoryCache.Set(cacheKey, report, TimeSpan.FromHours(24));
            }
            else
            {
                _memoryCache.Set(cacheKey, report, CacheDuration);
            }
            
            return report;
        }

        public async Task<vm_zreport> GetWeeklyZReport(Guid? officeId, DateTime weekStartDate)
        {
            var startDate = weekStartDate.Date;
            var endDate = startDate.AddDays(7).AddSeconds(-1);

            return await GenerateZReport(officeId, startDate, endDate, ZReportPeriod.Weekly);
        }

        public async Task<vm_zreport> GetMonthlyZReport(Guid? officeId, int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddSeconds(-1);

            return await GenerateZReport(officeId, startDate, endDate, ZReportPeriod.Monthly);
        }

        public async Task<vm_zreport> GetYearlyZReport(Guid? officeId, int year)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year + 1, 1, 1).AddSeconds(-1);

            return await GenerateZReport(officeId, startDate, endDate, ZReportPeriod.Yearly);
        }

        public async Task<vm_zreport> GetCustomPeriodZReport(Guid? officeId, DateTime startDate, DateTime endDate)
        {
            return await GenerateZReport(officeId, startDate, endDate, ZReportPeriod.Daily);
        }

        public async Task<List<vm_zreport>> GetHistoricalZReports(Guid? officeId, ZReportPeriod period, int count)
        {
            var reports = new List<vm_zreport>();
            var currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTz);

            for (int i = 0; i < count; i++)
            {
                vm_zreport report = null;

                switch (period)
                {
                    case ZReportPeriod.Daily:
                        var dailyDate = currentDate.AddDays(-i);
                        report = await GetDailyZReport(officeId, dailyDate);
                        break;

                    case ZReportPeriod.Weekly:
                        var weekStartDate = currentDate.AddDays(-(int)currentDate.DayOfWeek).AddDays(-7 * i);
                        report = await GetWeeklyZReport(officeId, weekStartDate);
                        break;

                    case ZReportPeriod.Monthly:
                        var monthDate = currentDate.AddMonths(-i);
                        report = await GetMonthlyZReport(officeId, monthDate.Year, monthDate.Month);
                        break;

                    case ZReportPeriod.Yearly:
                        report = await GetYearlyZReport(officeId, currentDate.Year - i);
                        break;
                }

                if (report != null)
                    reports.Add(report);
            }

            return reports;
        }

        private async Task<vm_zreport> GenerateZReport(Guid? officeId, DateTime startDate, DateTime endDate, ZReportPeriod period)
        {
            var report = new vm_zreport
            {
                OfficeId = officeId,
                ReportDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTz),
                Period = period,
                PeriodStart = startDate,
                PeriodEnd = endDate,
                CurrencyDetails = new List<vm_zreport_currency_detail>(),
                OfficeBreakdown = new List<vm_zreport_office_summary>(),
                VaultBalanceHistories = new List<vm_vaultbalancehistory>()
            };

            // If specific office, get office details
            if (officeId.HasValue)
            {
                var office = await _context.Offices.FirstOrDefaultAsync(o => o.Id == officeId.Value);
                if (office != null)
                {
                    report.OfficeName = office.OfficeName;

                    // Check if vault is open (vault is considered open if it's active)
                    var vault = await _context.Vaults
                        .FirstOrDefaultAsync(v => v.OfficeId == officeId.Value && v.IsActive);
                    report.IsVaultOpen = vault != null && vault.IsActive;
                }

                // Generate single office report
                await GenerateSingleOfficeReport(report, officeId.Value, startDate, endDate);
            }
            else
            {
                // Generate multi-office aggregated report
                await GenerateMultiOfficeReport(report, startDate, endDate);
            }

            return report;
        }

        private async Task GenerateSingleOfficeReport(vm_zreport report, Guid officeId, DateTime startDate, DateTime endDate)
        {
            // Get all transactions for the period with optimized query
            var transactions = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Details)
                    .ThenInclude(d => d.Currency)
                .Include(t => t.Vault)
                .Where(t => t.Vault.OfficeId == officeId &&
                          t.TransactionDate >= startDate &&
                          t.TransactionDate <= endDate &&
                          t.Status == TransactionStatus.Completed &&
                          !t.IsDeleted)
                .ToListAsync();


            // Get vault balance history for deposits and withdrawals (excluding ghost entries)
            var vaultHistories = await _context.VaultBalanceHistories
                .AsNoTracking()
                .Include(vh => vh.Currency)
                .Include(vh => vh.Vault)
                .Where(vh => vh.Vault.OfficeId == officeId &&
                            vh.CreatedDate >= startDate &&
                            vh.CreatedDate <= endDate &&
                            !vh.IsDeleted && 
                            !vh.IsGhost)
                .OrderByDescending(vh => vh.CreatedDate)
                .ToListAsync();
                
            // Kasa Hareketleri satırı genişletildiğinde uygulanan kur ve net kâr/zarar gösterebilmek
            // için VaultBalanceHistory.Description'daki "Exchange transaction {TransactionNumber}"
            // biçiminden gerçek Transaction kaydına geri bağlanılıyor (Details listesinden ilgili
            // para biriminin Rate'i, Transaction'dan da Profit alınıyor).
            var transactionsByNumber = transactions
                .Where(t => !string.IsNullOrEmpty(t.TransactionNumber))
                .GroupBy(t => t.TransactionNumber)
                .ToDictionary(g => g.Key, g => g.First());

            // Bir Satış işleminde "Alış Kuru" olarak GERÇEK işlem kurunu değil, o satıştan ÖNCEKİ
            // ortalama alış maliyetini (WAC) göstermemiz gerekiyor — aksi halde iki kur da aynı
            // (işlemin tek kuru) görünüp kâr/zarar farkı anlaşılmaz oluyor. Satış anında loglanan
            // CurrencyWacHistories.OldWac, tam olarak o satıştan hemen önceki maliyet tabanıdır.
            var reportTransactionIds = transactions.Select(t => t.Id).ToList();
            var wacAtSaleByTransactionCurrency = await _context.CurrencyWacHistories
                .Where(h => h.TransactionId != null && reportTransactionIds.Contains(h.TransactionId.Value))
                .GroupBy(h => new { h.TransactionId, h.CurrencyId })
                .Select(g => g.OrderBy(h => h.CreatedDate).First())
                .ToDictionaryAsync(h => (h.TransactionId!.Value, h.CurrencyId), h => h.OldWac);

            // Also prepare balance histories for the report
            var balanceHistoriesForReport = new List<vm_vaultbalancehistory>();
            
            // Pre-fetch users for vault histories
            var historyUserIds = vaultHistories.Where(h => h.UserId.HasValue).Select(h => h.UserId.Value).Distinct().ToList();
            var historyUsers = await _context.Users
                .AsNoTracking()
                .Where(u => historyUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Firstname + " " + u.Lastname);

            // Check for inherited balance from previous vault
            decimal openingBalanceTRY = 0;
            bool hasInheritedBalance = false;
            
            // Get the previous closed vault to check for inherited balance
            var previousClosedVault = await _context.Vaults
                .AsNoTracking()
                .Where(v => v.OfficeId == officeId && 
                           !v.IsActive && 
                           v.ClosedDate != null)
                .OrderByDescending(v => v.ClosedDate)
                .FirstOrDefaultAsync();
                
            if (previousClosedVault != null)
            {
                // Check if current active vault was created after this closed vault
                var currentVault = await _context.Vaults
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.OfficeId == officeId && v.IsActive);
                    
                if (currentVault != null && currentVault.CreatedDate > previousClosedVault.ClosedDate)
                {
                    openingBalanceTRY = previousClosedVault.ClosingBalance;
                    hasInheritedBalance = true;
                }
            }
            
            // Initialize summary
            var summary = new vm_zreport_summary
            {
                TotalTransactions = transactions.Count,
                TotalExchangeTransactions = transactions.Count(t => t.Type == TransactionType.Exchange || t.Type == TransactionType.Buy),
                TotalDepositTransactions = transactions.Count(t => t.Type == TransactionType.Deposit),
                TotalWithdrawalTransactions = transactions.Count(t => t.Type == TransactionType.Withdrawal),
                TotalVolumesByCurrency = new Dictionary<string, decimal>(),
                OpeningBalanceTRY = openingBalanceTRY,
                HasInheritedBalance = hasInheritedBalance
            };

            // Initialize currency tracking
            var currencyDetailsMap = new Dictionary<string, vm_zreport_currency_detail>();

            // Initialize party summary
            var partySummary = new vm_zreport_party_summary
            {
                TotalDebtsByCurrency = new Dictionary<string, decimal>(),
                TotalReceivablesByCurrency = new Dictionary<string, decimal>()
            };

            // Initialize cash summary
            var cashSummary = new vm_zreport_cash_summary
            {
                CashVolumesByCurrency = new Dictionary<string, decimal>(),
                VaultBalancesByCurrency = new Dictionary<string, decimal>()
            };

            // Ciro/İşlem Hacmi (TotalForeignCurrencyProcessed) her işlemde BİR kez artırılmalı.
            // Normal alım-satımda zaten tek yabancı bacak var (TRY bacağı yukarıda atlanıyor), ama
            // arbitraj (çapraz kur) işlemlerinde 2 yabancı bacak var — ikisi de aşağıdaki döngüde
            // işlendiği için düzeltme olmadan tek bir arbitraj işlemi ciroyu iki katı artırıyordu.
            var volumeCountedTransactionIds = new HashSet<Guid>();

            // Process transactions
            foreach (var transaction in transactions)
            {
                // Add to total profit
                if (transaction.Type == TransactionType.Exchange || transaction.Type == TransactionType.Buy)
                {
                    summary.TotalProfit += transaction.Profit;

                    // Track cash vs party transactions
                    if (transaction.PartyId == null)
                    {
                        cashSummary.CashProfit += transaction.Profit;
                        cashSummary.CashTransactionCount++;
                    }
                    else
                    {
                        partySummary.PartyTransactionCount++;
                    }
                }

                // İşlemin kaç yabancı (TRY-olmayan) bacağı var? Arbitraj (çapraz kur) işlemlerinde 2
                // yabancı bacak olur; normal alım-satımda 1 yabancı + 1 TRY bacağı olur.
                var foreignLegCount = transaction.Details.Count(d => d.Currency.CurrencyCode != "TRY");
                var isArbitrage = foreignLegCount >= 2;

                if ((transaction.Type == TransactionType.Exchange || transaction.Type == TransactionType.Buy) && isArbitrage)
                {
                    summary.TotalArbitrageProfit += transaction.Profit;
                }

                // Process transaction details
                foreach (var detail in transaction.Details)
                {
                    var currencyCode = detail.Currency.CurrencyCode;

                    // TRY, alınıp satılan bir "döviz" değil — sadece karşı bacağın (yabancı para biriminin)
                    // bedelidir. TRY'yi kendi satırı olarak Ciro/döviz tablosuna sokmak, o para biriminin
                    // TRY karşılığını sanki ayrı bir işlemmiş gibi ikinci kez göstermek anlamına gelir.
                    if (currencyCode == "TRY")
                        continue;

                    // Initialize currency detail if needed
                    if (!currencyDetailsMap.ContainsKey(currencyCode))
                    {
                        currencyDetailsMap[currencyCode] = new vm_zreport_currency_detail
                        {
                            CurrencyCode = currencyCode,
                            CurrencyName = detail.Currency.CurrencyName
                        };
                    }

                    var currencyDetail = currencyDetailsMap[currencyCode];
                    var amount = Math.Abs(detail.Amount);
                    var amountInTRY = amount * detail.Rate;

                    // For exchange transactions
                    if (transaction.Type == TransactionType.Exchange || transaction.Type == TransactionType.Buy)
                    {
                        // Update total volumes — sadece gerçek müşteri alım-satımı (Exchange/Buy) hacme
                        // sayılır; Transfer/Deposit/Withdrawal gerçek bir alış-satış değildir.
                        if (!summary.TotalVolumesByCurrency.ContainsKey(currencyCode))
                            summary.TotalVolumesByCurrency[currencyCode] = 0;
                        summary.TotalVolumesByCurrency[currencyCode] += amount;

                        if (volumeCountedTransactionIds.Add(transaction.Id))
                            summary.TotalForeignCurrencyProcessed += amountInTRY;

                        // Correct interpretation from exchange office perspective:
                        // Debit = We are giving out/selling this currency
                        // Credit = We are receiving/buying this currency
                        if (detail.Side == TransactionSide.Credit) // We bought/received this currency
                        {
                            currencyDetail.TotalBoughtAmount += amount;
                            currencyDetail.TotalBuyCost += amountInTRY;
                            currencyDetail.BuyTransactionCount++;
                        }
                        else // We sold/gave out this currency (Debit)
                        {
                            currencyDetail.TotalSoldAmount += amount;
                            currencyDetail.TotalSellRevenue += amountInTRY;
                            currencyDetail.SellTransactionCount++;
                        }

                        // Track cash volumes
                        if (transaction.PartyId == null)
                        {
                            if (!cashSummary.CashVolumesByCurrency.ContainsKey(currencyCode))
                                cashSummary.CashVolumesByCurrency[currencyCode] = 0;
                            cashSummary.CashVolumesByCurrency[currencyCode] += amount;
                            cashSummary.CashVolumeInTRY += amountInTRY;
                        }
                        else
                        {
                            partySummary.PartyTransactionVolume += amountInTRY;
                        }
                    }
                }
            }

            // Allocate transaction profits to foreign currencies only. Arbitraj (çapraz kur) işlemlerinin
            // İKİ yabancı bacağı olduğundan, kârı rastgele "ilk bulunan" para birimine atamak yanıltıcıdır
            // (bkz. summary.TotalArbitrageProfit — bu işlemlerin kârı orada ayrıca toplanıyor). Bu yüzden
            // sadece TEK yabancı bacaklı (normal alım-satım) işlemler burada para birimine atanır.
            var transactionProfitsByForeignCurrency = new Dictionary<string, decimal>();
            foreach (var transaction in transactions.Where(t => t.Type == TransactionType.Exchange || t.Type == TransactionType.Buy))
            {
                var foreignCurrencyDetails = transaction.Details.Where(d => d.Currency.CurrencyCode != "TRY").ToList();
                if (foreignCurrencyDetails.Count == 1)
                {
                    var foreignCurrencyCode = foreignCurrencyDetails[0].Currency.CurrencyCode;
                    if (!transactionProfitsByForeignCurrency.ContainsKey(foreignCurrencyCode))
                        transactionProfitsByForeignCurrency[foreignCurrencyCode] = 0;

                    transactionProfitsByForeignCurrency[foreignCurrencyCode] += transaction.Profit;
                }
            }

            // Pre-fetch data to avoid N+1 queries in loops below
            var allCurrencies = await _context.Currencies.AsNoTracking().ToDictionaryAsync(c => c.CurrencyCode, c => c);
            var tryCurrency = allCurrencies.GetValueOrDefault("TRY");
            var tryCurrencyId = tryCurrency?.Id ?? Guid.Empty;

            var mainVault = await _context.Vaults.AsNoTracking()
                .FirstOrDefaultAsync(v => v.OfficeId == officeId && v.IsActive && v.Type == Vault.VaultType.Main);

            var allExchangeRates = await _context.ExchangeRates
                .Where(r => r.OfficeId == officeId && r.TargetCurrencyId == tryCurrencyId && r.IsActive)
                .GroupBy(r => r.SourceCurrencyId)
                .Select(g => g.OrderByDescending(r => r.EffectiveFrom).FirstOrDefault())
                .ToDictionaryAsync(r => r!.SourceCurrencyId, r => r!);

            var wacDict = mainVault != null
                ? await _wacService.GetAllWacsForVaultAsync(mainVault.Id)
                : new Dictionary<Guid, decimal>();

            var vaultBalanceDict = mainVault != null
                ? await _context.VaultBalances.AsNoTracking()
                    .Where(vb => vb.VaultId == mainVault.Id)
                    .ToDictionaryAsync(vb => vb.CurrencyId, vb => vb.Balance)
                : new Dictionary<Guid, decimal>();

            // Calculate currency performance metrics
            foreach (var kvp in currencyDetailsMap)
            {
                var detail = kvp.Value;
                var currencyCode = kvp.Key;

                // Calculate averages
                if (detail.BuyTransactionCount > 0 && detail.TotalBoughtAmount > 0)
                    detail.AverageBuyRate = detail.TotalBuyCost / detail.TotalBoughtAmount;

                if (detail.SellTransactionCount > 0 && detail.TotalSoldAmount > 0)
                    detail.AverageSellRate = detail.TotalSellRevenue / detail.TotalSoldAmount;

                // Assign profit only to foreign currencies (non-TRY)
                if (currencyCode == "TRY")
                {
                    detail.Profit = 0;
                }
                else if (transactionProfitsByForeignCurrency.ContainsKey(currencyCode))
                {
                    detail.Profit = transactionProfitsByForeignCurrency[currencyCode];
                }
                else
                {
                    detail.Profit = detail.TotalSellRevenue - detail.TotalBuyCost;
                }

                // Marj = Kâr / Satış Hasılatı — o gün satılan miktarın TL karşılığına göre standart kâr marjı.
                // Önceki formül (Kâr / Alış Maliyeti) alım hacmi düşük/sıfırken (ör. eski ucuz stoktan satış
                // yapılan bir günde) anlamsız yüzdeler üretiyordu (ör. %295,6).
                if (detail.TotalSellRevenue > 0 && currencyCode != "TRY")
                    detail.ProfitMargin = (detail.Profit / detail.TotalSellRevenue) * 100;
                else
                    detail.ProfitMargin = 0;

                // Net position
                detail.NetPosition = detail.TotalBoughtAmount - detail.TotalSoldAmount;

                // Get current rates from pre-fetched data
                if (allCurrencies.TryGetValue(kvp.Key, out var curr) && curr.Id != tryCurrencyId
                    && allExchangeRates.TryGetValue(curr.Id, out var currentRate))
                {
                    detail.CurrentBuyRate = currentRate.BuyRate;
                    detail.CurrentSellRate = currentRate.SellRate;
                    detail.Spread = currentRate.SellRate - currentRate.BuyRate;
                }

                // WAC data from pre-fetched dictionaries
                if (currencyCode != "TRY" && allCurrencies.TryGetValue(currencyCode, out var wacCurrency) && mainVault != null)
                {
                    detail.Wac = wacDict.GetValueOrDefault(wacCurrency.Id);
                    detail.CurrentBalance = vaultBalanceDict.GetValueOrDefault(wacCurrency.Id);
                    detail.RealizedProfit = detail.Profit;
                    if (detail.Wac > 0 && detail.CurrentBalance > 0 && detail.CurrentSellRate > 0)
                        detail.UnrealizedProfit = (detail.CurrentSellRate - detail.Wac) * detail.CurrentBalance;
                }
            }

            // O gün hiç işlem görmemiş ama kasada bakiyesi duran para birimleri de rapora dahil edilir —
            // aksi halde "Tüm Şubeler" görünümünde bu bakiyeler sessizce kaybolur (bkz. denetim bulgusu).
            if (mainVault != null)
            {
                foreach (var kvp in vaultBalanceDict)
                {
                    if (kvp.Value <= 0)
                        continue;

                    var idleCurrency = allCurrencies.Values.FirstOrDefault(c => c.Id == kvp.Key);
                    if (idleCurrency == null || idleCurrency.CurrencyCode == "TRY" || currencyDetailsMap.ContainsKey(idleCurrency.CurrencyCode))
                        continue;

                    var idleDetail = new vm_zreport_currency_detail
                    {
                        CurrencyCode = idleCurrency.CurrencyCode,
                        CurrencyName = idleCurrency.CurrencyName,
                        Wac = wacDict.GetValueOrDefault(kvp.Key),
                        CurrentBalance = kvp.Value
                    };

                    if (allExchangeRates.TryGetValue(kvp.Key, out var idleRate))
                    {
                        idleDetail.CurrentBuyRate = idleRate.BuyRate;
                        idleDetail.CurrentSellRate = idleRate.SellRate;
                        idleDetail.Spread = idleRate.SellRate - idleRate.BuyRate;
                        if (idleDetail.Wac > 0 && idleDetail.CurrentSellRate > 0)
                            idleDetail.UnrealizedProfit = (idleDetail.CurrentSellRate - idleDetail.Wac) * idleDetail.CurrentBalance;
                    }

                    currencyDetailsMap[idleCurrency.CurrencyCode] = idleDetail;
                }
            }

            // Get party account balances
            var partyAccounts = await _context.PartyAccounts
                .Include(pa => pa.Currency)
                .Include(pa => pa.Party)
                .Where(pa => pa.Party.OfficeId == officeId && pa.Balance != 0)
                .ToListAsync();

            partySummary.TotalPartyAccounts = partyAccounts.Count;
            partySummary.ActivePartyAccounts = partyAccounts.Count(pa => pa.Balance != 0);

            foreach (var account in partyAccounts)
            {
                var currencyCode = account.Currency.CurrencyCode;
                var balance = account.Balance;
                var rateToTRY = account.CurrencyId == tryCurrencyId ? 1m
                    : allExchangeRates.TryGetValue(account.CurrencyId, out var rateObj) ? rateObj.BuyRate : 0m;
                var balanceInTRY = Math.Abs(balance) * rateToTRY;

                if (balance < 0) // We owe to party
                {
                    if (!partySummary.TotalDebtsByCurrency.ContainsKey(currencyCode))
                        partySummary.TotalDebtsByCurrency[currencyCode] = 0;
                    partySummary.TotalDebtsByCurrency[currencyCode] += Math.Abs(balance);
                    partySummary.TotalDebtsInTRY += balanceInTRY;
                }
                else // Party owes to us
                {
                    if (!partySummary.TotalReceivablesByCurrency.ContainsKey(currencyCode))
                        partySummary.TotalReceivablesByCurrency[currencyCode] = 0;
                    partySummary.TotalReceivablesByCurrency[currencyCode] += balance;
                    partySummary.TotalReceivablesInTRY += balanceInTRY;
                }
            }

            partySummary.NetPositionInTRY = partySummary.TotalReceivablesInTRY - partySummary.TotalDebtsInTRY;

            // Get vault balances
            var vaultBalances = await _context.VaultBalances
                .Include(vb => vb.Currency)
                .Include(vb => vb.Vault)
                .Where(vb => vb.Vault.OfficeId == officeId && vb.Vault.IsActive)
                .ToListAsync();

            decimal totalValueInBaseCurrency = 0;
            foreach (var balance in vaultBalances)
            {
                var currencyCode = balance.Currency.CurrencyCode;
                cashSummary.VaultBalancesByCurrency[currencyCode] = balance.Balance;
                var vbRate = balance.CurrencyId == tryCurrencyId ? 1m
                    : allExchangeRates.TryGetValue(balance.CurrencyId, out var vbRateObj) ? vbRateObj.BuyRate : 0m;
                var valueInTRY = balance.Balance * vbRate;
                cashSummary.TotalVaultValueInTRY += valueInTRY;
                totalValueInBaseCurrency += valueInTRY;
            }

            // Kasa Hareketleri tablosunda her satır için "Bakiye" (o kasa+para birimindeki işlem
            // sonrası bakiye) gösterilir. Bunu geçmiş hareketlerin toplamından (açılış bakiyesi +
            // kümülatif toplam) ileri doğru hesaplamak YANLIŞ — VaultBalanceHistory tablosundaki
            // geçmiş kayıtlar (eski test senaryoları, elle düzeltmeler vb.) zamanla gerçek kasa
            // bakiyesinden (VaultBalance.Balance) sapabiliyor; bu sapma ileri yönlü toplamda hiç
            // düzelmeden sonsuza kadar taşınır. Bunun yerine ŞU AN doğrulanmış gerçek bakiyeye
            // (vaultBalances, yukarıda zaten çekildi) ÇAPA atılıp, raporun bitiş tarihinden SONRA
            // olan hareketler çıkarılarak "rapor bitiminde gerçek bakiye" bulunur; oradan geriye
            // doğru (en yeniden en eskiye) her hareketin kendi tutarı düşülerek satır satır bakiye
            // hesaplanır. Bu yöntem geçmişteki herhangi bir tutarsızlıktan etkilenmez, çünkü hep
            // canlı/doğrulanmış bakiyeden başlar.
            var currentBalanceByKey = vaultBalances.ToDictionary(vb => (vb.VaultId, vb.CurrencyId), vb => vb.Balance);

            var vaultCurrencyPairs = vaultHistories.Select(vh => new { vh.VaultId, vh.CurrencyId }).Distinct().ToList();
            var pairVaultIds = vaultCurrencyPairs.Select(p => p.VaultId).Distinct().ToList();
            var pairCurrencyIds = vaultCurrencyPairs.Select(p => p.CurrencyId).Distinct().ToList();
            var afterEndDateRows = await _context.VaultBalanceHistories
                .Where(vh => pairVaultIds.Contains(vh.VaultId) && pairCurrencyIds.Contains(vh.CurrencyId) &&
                             vh.CreatedDate > endDate && !vh.IsDeleted && !vh.IsGhost)
                .GroupBy(vh => new { vh.VaultId, vh.CurrencyId })
                .Select(g => new { g.Key.VaultId, g.Key.CurrencyId, Sum = g.Sum(x => x.Balance) })
                .ToListAsync();
            var sumAfterEndDate = afterEndDateRows.ToDictionary(r => (r.VaultId, r.CurrencyId), r => r.Sum);

            var runningBalanceById = new Dictionary<Guid, decimal>();
            var runningBalanceCursor = new Dictionary<(Guid, Guid), decimal>();
            foreach (var vh in vaultHistories.OrderByDescending(vh => vh.CreatedDate))
            {
                var key = (vh.VaultId, vh.CurrencyId);
                if (!runningBalanceCursor.TryGetValue(key, out var cursor))
                {
                    var currentBalance = currentBalanceByKey.GetValueOrDefault(key);
                    var afterEnd = sumAfterEndDate.GetValueOrDefault(key);
                    cursor = currentBalance - afterEnd; // rapor bitiş tarihindeki gerçek bakiye
                }
                runningBalanceById[vh.Id] = cursor;
                runningBalanceCursor[key] = cursor - vh.Balance;
            }

            // Calculate vault operations and build history list
            decimal totalDeposits = 0m;
            decimal totalWithdrawals = 0m;

            foreach (var vaultHistory in vaultHistories)
            {
                // "Exchange transaction {TransactionNumber}" açıklamasından gerçek işlemi bulup
                // bu para birimi bacağının uygulanan kurunu ve işlemin net kâr/zararını ekliyoruz.
                decimal? appliedRate = null;
                decimal? costBasisRate = null;
                decimal? transactionProfit = null;
                if (vaultHistory.Description != null && vaultHistory.Description.StartsWith("Exchange transaction "))
                {
                    var txNumber = vaultHistory.Description.Substring("Exchange transaction ".Length).Trim();
                    if (transactionsByNumber.TryGetValue(txNumber, out var matchedTx))
                    {
                        transactionProfit = matchedTx.Profit;
                        var matchedDetail = matchedTx.Details?.FirstOrDefault(d => d.CurrencyId == vaultHistory.CurrencyId);
                        appliedRate = matchedDetail?.Rate;

                        // Bu bacak, kasadan SATILAN (Debit) bir döviz ise "Alış Kuru" olarak işlemin
                        // kendi kurunu değil, satıştan hemen önceki ortalama alış maliyetini (WAC)
                        // göster — aksi halde alış/satış kuru hep aynı görünüp kâr mantıksız kalıyor.
                        if (matchedDetail != null && matchedDetail.Side == TransactionSide.Debit
                            && vaultHistory.CurrencyId != tryCurrencyId
                            && wacAtSaleByTransactionCurrency.TryGetValue((matchedTx.Id, vaultHistory.CurrencyId), out var wacAtSale))
                        {
                            costBasisRate = wacAtSale;
                        }
                    }
                }

                // Değerleme, personelin işlemde GERÇEKTEN kullandığı kuru baz alır (appliedRate);
                // yalnızca bir işlem eşleşmediğinde (manuel yatırma/çekme/düzeltme vb.) genel piyasa
                // kuruna düşülür. Bu, KRUB/MEUR gibi otomatik kur kaynağı olmayan birimlerde de
                // değerlemenin 0 TRY görünmesini engeller.
                decimal histRate = vaultHistory.CurrencyId == tryCurrencyId ? 1m
                    : appliedRate ?? (allExchangeRates.TryGetValue(vaultHistory.CurrencyId, out var histRateObj) ? histRateObj.BuyRate : 0m);

                decimal amountInTRY = Math.Abs(vaultHistory.Balance) * histRate;

                if (vaultHistory.TransactionType == TransactionType.Deposit)
                {
                    totalDeposits += amountInTRY;
                }
                else if (vaultHistory.TransactionType == TransactionType.Withdrawal)
                {
                    totalWithdrawals += amountInTRY;
                }

                // Add to balance histories for report
                var historyItem = new vm_vaultbalancehistory
                {
                    Id = vaultHistory.Id,
                    CurrencyId = vaultHistory.CurrencyId,
                    CurrencyCode = vaultHistory.Currency.CurrencyCode,
                    CurrencyName = vaultHistory.Currency.CurrencyName,
                    Balance = vaultHistory.Balance,
                    Description = vaultHistory.Description,
                    TransactionType = vaultHistory.TransactionType,
                    CreatedDate = vaultHistory.CreatedDate,
                    ValueInBaseCurrency = amountInTRY,
                    RunningBalance = runningBalanceById.GetValueOrDefault(vaultHistory.Id),
                    IsParty = vaultHistory.IsParty,
                    AppliedRate = appliedRate,
                    CostBasisRate = costBasisRate,
                    TransactionProfit = transactionProfit,
                    User = vaultHistory.UserId.HasValue && historyUsers.ContainsKey(vaultHistory.UserId.Value)
                        ? historyUsers[vaultHistory.UserId.Value]
                        : "System"
                };
                balanceHistoriesForReport.Add(historyItem);
            }

            // Set vault operations in summary
            summary.VaultDeposits = totalDeposits;
            summary.VaultWithdrawals = totalWithdrawals;
            summary.NetVaultChange = totalDeposits - totalWithdrawals;
            summary.ProfitAfterVaultOperations = summary.TotalProfit + summary.NetVaultChange;

            // Calculate final summary metrics
            summary.TotalProfitInTRY = summary.TotalProfit; // Assuming profit is already in TRY
            summary.TotalValueInBaseCurrency = totalValueInBaseCurrency; // Set total vault value in TRY
            if (summary.TotalExchangeTransactions > 0)
                summary.AverageTransactionSize = summary.TotalForeignCurrencyProcessed / summary.TotalExchangeTransactions;
            if (summary.TotalForeignCurrencyProcessed > 0)
                summary.ProfitMargin = (summary.TotalProfit / summary.TotalForeignCurrencyProcessed) * 100;

            // Personel bazlı kırılım: kimin ne kadar işlem yapıp ne kadar kâr getirdiği. Sadece tek-şube
            // görünümünde anlamlıdır (bkz. GenerateMultiOfficeReport — orada boş liste döner, çünkü personel
            // tek şubeye özgüdür ve şubeler arası toplamak yanıltıcı bir "şirket geneli liderlik tablosu"
            // izlenimi verir).
            var employeeUserIds = transactions.Select(t => t.UserId).Distinct().ToList();
            var employeeUsers = await _context.Users
                .AsNoTracking()
                .Where(u => employeeUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Firstname + " " + u.Lastname);

            report.EmployeeBreakdown = transactions
                .GroupBy(t => t.UserId)
                .Select(g =>
                {
                    var exchangeTxs = g.Where(t => t.Type == TransactionType.Exchange || t.Type == TransactionType.Buy).ToList();
                    var volumeInTRY = exchangeTxs
                        .SelectMany(t => t.Details)
                        .Where(d => d.Currency.CurrencyCode != "TRY")
                        .Sum(d => Math.Abs(d.Amount) * d.Rate);
                    return new vm_zreport_employee_summary
                    {
                        UserId = g.Key,
                        EmployeeName = employeeUsers.GetValueOrDefault(g.Key, "Bilinmeyen"),
                        TransactionCount = g.Count(),
                        ExchangeTransactionCount = exchangeTxs.Count,
                        TotalProfit = exchangeTxs.Sum(t => t.Profit),
                        TotalVolumeInTRY = volumeInTRY,
                        AverageTransactionSize = exchangeTxs.Count > 0 ? volumeInTRY / exchangeTxs.Count : 0
                    };
                })
                .OrderByDescending(e => e.TotalProfit)
                .ToList();

            // Assign to report
            report.Summary = summary;
            report.CurrencyDetails = currencyDetailsMap.Values.OrderByDescending(cd => cd.Profit).ToList();
            report.PartyAccountsSummary = partySummary;
            report.CashOnlySummary = cashSummary;
            report.VaultBalanceHistories = balanceHistoriesForReport.OrderByDescending(h => h.CreatedDate).ToList();
        }

        private async Task GenerateMultiOfficeReport(vm_zreport report, DateTime startDate, DateTime endDate)
        {
            // Get all offices
            var offices = await _context.Offices.Where(o => o.IsActive).ToListAsync();

            var totalSummary = new vm_zreport_summary
            {
                TotalVolumesByCurrency = new Dictionary<string, decimal>()
            };

            var aggregatedCurrencyDetails = new Dictionary<string, vm_zreport_currency_detail>();
            // WAC ofisler arasında toplanamaz (ağırlıklı ortalama gerekir) — maliyet tabanını (Wac × Bakiye)
            // biriktirip sonda bakiyeye bölerek doğru bir ağırlıklı ortalama WAC elde ediyoruz.
            var wacCostBasisByCurrency = new Dictionary<string, decimal>();
            var aggregatedBalanceHistories = new List<vm_vaultbalancehistory>();
            var totalPartyDebtsInTRY = 0m;
            var totalPartyReceivablesInTRY = 0m;
            var totalCashProfit = 0m;
            var totalCashVolume = 0m;
            var totalVaultValue = 0m;
            var totalCashTransactionCount = 0;

            foreach (var office in offices)
            {
                // Generate report for each office
                var officeReport = new vm_zreport();
                await GenerateSingleOfficeReport(officeReport, office.Id, startDate, endDate);

                // Check vault status for this office
                var officeVault = await _context.Vaults
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.OfficeId == office.Id && v.IsActive);
                var officeVaultOpen = officeVault != null;

                // Create office summary
                var officeSummary = new vm_zreport_office_summary
                {
                    OfficeId = office.Id,
                    OfficeName = office.OfficeName,
                    Profit = officeReport.Summary?.TotalProfit ?? 0,
                    TransactionCount = officeReport.Summary?.TotalTransactions ?? 0,
                    VolumeInTRY = officeReport.Summary?.TotalForeignCurrencyProcessed ?? 0,
                    IsVaultOpen = officeVaultOpen
                };

                if (officeVaultOpen)
                    report.IsVaultOpen = true;

                report.OfficeBreakdown.Add(officeSummary);

                // Aggregate totals
                if (officeReport.Summary != null)
                {
                    totalSummary.TotalProfit += officeReport.Summary.TotalProfit;
                    totalSummary.TotalTransactions += officeReport.Summary.TotalTransactions;
                    totalSummary.TotalExchangeTransactions += officeReport.Summary.TotalExchangeTransactions;
                    totalSummary.TotalDepositTransactions += officeReport.Summary.TotalDepositTransactions;
                    totalSummary.TotalWithdrawalTransactions += officeReport.Summary.TotalWithdrawalTransactions;
                    totalSummary.TotalForeignCurrencyProcessed += officeReport.Summary.TotalForeignCurrencyProcessed;
                    totalSummary.TotalArbitrageProfit += officeReport.Summary.TotalArbitrageProfit;

                    // Aggregate vault operations
                    totalSummary.VaultDeposits += officeReport.Summary.VaultDeposits;
                    totalSummary.VaultWithdrawals += officeReport.Summary.VaultWithdrawals;
                    totalSummary.NetVaultChange += officeReport.Summary.NetVaultChange;

                    // Aggregate volumes by currency
                    foreach (var kvp in officeReport.Summary.TotalVolumesByCurrency)
                    {
                        if (!totalSummary.TotalVolumesByCurrency.ContainsKey(kvp.Key))
                            totalSummary.TotalVolumesByCurrency[kvp.Key] = 0;
                        totalSummary.TotalVolumesByCurrency[kvp.Key] += kvp.Value;
                    }
                }

                // Aggregate currency details
                if (officeReport.CurrencyDetails != null)
                {
                    foreach (var detail in officeReport.CurrencyDetails)
                    {
                        if (!aggregatedCurrencyDetails.ContainsKey(detail.CurrencyCode))
                        {
                            aggregatedCurrencyDetails[detail.CurrencyCode] = new vm_zreport_currency_detail
                            {
                                CurrencyCode = detail.CurrencyCode,
                                CurrencyName = detail.CurrencyName,
                                CurrentBuyRate = detail.CurrentBuyRate,
                                CurrentSellRate = detail.CurrentSellRate,
                                Spread = detail.Spread
                            };
                        }

                        var aggDetail = aggregatedCurrencyDetails[detail.CurrencyCode];
                        aggDetail.TotalBoughtAmount += detail.TotalBoughtAmount;
                        aggDetail.TotalBuyCost += detail.TotalBuyCost;
                        aggDetail.BuyTransactionCount += detail.BuyTransactionCount;
                        aggDetail.TotalSoldAmount += detail.TotalSoldAmount;
                        aggDetail.TotalSellRevenue += detail.TotalSellRevenue;
                        aggDetail.SellTransactionCount += detail.SellTransactionCount;
                        aggDetail.Profit += detail.Profit;
                        aggDetail.NetPosition += detail.NetPosition;
                        aggDetail.CurrentBalance += detail.CurrentBalance;

                        if (detail.Wac > 0 && detail.CurrentBalance > 0)
                        {
                            wacCostBasisByCurrency.TryGetValue(detail.CurrencyCode, out var existingCostBasis);
                            wacCostBasisByCurrency[detail.CurrencyCode] = existingCostBasis + (detail.Wac * detail.CurrentBalance);
                        }
                    }
                }

                // Aggregate party accounts
                if (officeReport.PartyAccountsSummary != null)
                {
                    totalPartyDebtsInTRY += officeReport.PartyAccountsSummary.TotalDebtsInTRY;
                    totalPartyReceivablesInTRY += officeReport.PartyAccountsSummary.TotalReceivablesInTRY;
                }

                // Aggregate cash summary
                if (officeReport.CashOnlySummary != null)
                {
                    totalCashProfit += officeReport.CashOnlySummary.CashProfit;
                    totalCashVolume += officeReport.CashOnlySummary.CashVolumeInTRY;
                    totalVaultValue += officeReport.CashOnlySummary.TotalVaultValueInTRY;
                    totalCashTransactionCount += officeReport.CashOnlySummary.CashTransactionCount;
                }
                
                // Aggregate balance histories
                if (officeReport.VaultBalanceHistories != null)
                {
                    aggregatedBalanceHistories.AddRange(officeReport.VaultBalanceHistories);
                }
            }

            // Calculate contribution percentages
            foreach (var officeSummary in report.OfficeBreakdown)
            {
                if (totalSummary.TotalProfit > 0)
                    officeSummary.ContributionPercentage = (officeSummary.Profit / totalSummary.TotalProfit) * 100;
            }

            // Finalize aggregated currency details
            foreach (var detail in aggregatedCurrencyDetails.Values)
            {
                if (detail.BuyTransactionCount > 0)
                    detail.AverageBuyRate = detail.TotalBuyCost / detail.TotalBoughtAmount;
                if (detail.SellTransactionCount > 0)
                    detail.AverageSellRate = detail.TotalSellRevenue / detail.TotalSoldAmount;
                if (detail.TotalSellRevenue > 0)
                    detail.ProfitMargin = (detail.Profit / detail.TotalSellRevenue) * 100;

                if (detail.CurrentBalance > 0 && wacCostBasisByCurrency.TryGetValue(detail.CurrencyCode, out var costBasis))
                {
                    detail.Wac = costBasis / detail.CurrentBalance;
                    if (detail.Wac > 0 && detail.CurrentSellRate > 0)
                        detail.UnrealizedProfit = (detail.CurrentSellRate - detail.Wac) * detail.CurrentBalance;
                }
            }

            // Calculate final summary metrics including vault operations
            totalSummary.TotalProfitInTRY = totalSummary.TotalProfit;
            totalSummary.ProfitAfterVaultOperations = totalSummary.TotalProfit + totalSummary.NetVaultChange;
            if (totalSummary.TotalExchangeTransactions > 0)
                totalSummary.AverageTransactionSize = totalSummary.TotalForeignCurrencyProcessed / totalSummary.TotalExchangeTransactions;
            if (totalSummary.TotalForeignCurrencyProcessed > 0)
                totalSummary.ProfitMargin = (totalSummary.TotalProfit / totalSummary.TotalForeignCurrencyProcessed) * 100;

            // Create aggregated party summary
            var totalPartySummary = new vm_zreport_party_summary
            {
                TotalDebtsInTRY = totalPartyDebtsInTRY,
                TotalReceivablesInTRY = totalPartyReceivablesInTRY,
                NetPositionInTRY = totalPartyReceivablesInTRY - totalPartyDebtsInTRY
            };

            // Create aggregated cash summary
            var totalCashSummary = new vm_zreport_cash_summary
            {
                CashProfit = totalCashProfit,
                CashVolumeInTRY = totalCashVolume,
                TotalVaultValueInTRY = totalVaultValue,
                CashTransactionCount = totalCashTransactionCount
            };

            report.OfficeName = "All Offices";
            report.Summary = totalSummary;
            report.CurrencyDetails = aggregatedCurrencyDetails.Values.OrderByDescending(cd => cd.Profit).ToList();
            report.PartyAccountsSummary = totalPartySummary;
            report.CashOnlySummary = totalCashSummary;
            report.VaultBalanceHistories = aggregatedBalanceHistories.OrderByDescending(h => h.CreatedDate).ToList();

            // Personel bazlı kırılım "Tüm Şubeler" görünümünde gösterilmez — personel tek şubeye özgüdür,
            // şubeler arası toplamak yanıltıcı olur (bkz. GenerateSingleOfficeReport'taki açıklama).
            report.EmployeeBreakdown = new List<vm_zreport_employee_summary>();
        }

        private async Task<decimal> GetExchangeRateToTRY(Guid officeId, Guid currencyId)
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

        private async Task<Entity.Entities.ExchangeOffice.Currency.ExchangeRate> GetCurrentExchangeRate(Guid officeId, string currencyCode)
        {
            var currency = await _context.Currencies
                .FirstOrDefaultAsync(c => c.CurrencyCode == currencyCode);

            if (currency == null)
                return null;

            var baseCurrency = await _context.Currencies
                .FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");

            if (baseCurrency == null || currency.Id == baseCurrency.Id)
                return null;

            return await _context.ExchangeRates
                .Where(r => r.OfficeId == officeId &&
                           r.SourceCurrencyId == currency.Id &&
                           r.TargetCurrencyId == baseCurrency.Id &&
                           r.IsActive)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync();
        }

        public async Task EndDay(Guid officeId)
        {
            var dbActiveVaults = await _context.Vaults
                .Include(v => v.Balances)
                    .ThenInclude(b => b.Currency)
                .Where(x => x.OfficeId == officeId && x.IsActive)
                .ToListAsync();

            if (dbActiveVaults.Count == 0)
                throw new ApiException(HttpStatusCode.NotFound, "No active vault found for this office");

            var tryCurrency = await _context.Currencies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
            var tryCurrencyId = tryCurrency?.Id ?? Guid.Empty;

            var rateDict = await _context.ExchangeRates
                .Where(r => r.OfficeId == officeId && r.TargetCurrencyId == tryCurrencyId && r.IsActive)
                .GroupBy(r => r.SourceCurrencyId)
                .Select(g => g.OrderByDescending(r => r.EffectiveFrom).FirstOrDefault())
                .ToDictionaryAsync(r => r!.SourceCurrencyId, r => r!.BuyRate);

            foreach (var vault in dbActiveVaults)
            {
                decimal vaultValueInTRY = 0;
                foreach (var balance in vault.Balances)
                {
                    var rate = balance.CurrencyId == tryCurrencyId ? 1m
                        : rateDict.GetValueOrDefault(balance.CurrencyId);
                    vaultValueInTRY += balance.Balance * rate;
                }

                vault.IsActive = false;
                vault.ClosingBalance = vaultValueInTRY;
                vault.ClosedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private static string GetDailyCacheKey(Guid? officeId, DateTime date)
        {
            var startDate = date.Date;
            return $"ZReport_Daily_{officeId}_{startDate:yyyyMMdd}";
        }
        
        public void InvalidateDailyZReportCache(Guid? officeId, DateTime date)
        {
            var cacheKey = GetDailyCacheKey(officeId, date);
            _memoryCache.Remove(cacheKey);
        }
    }


}