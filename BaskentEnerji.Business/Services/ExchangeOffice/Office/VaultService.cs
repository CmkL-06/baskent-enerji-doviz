using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using BaskentEnerji.Entity;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Office
{
    public class VaultService : IVaultService
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _memoryCache;
        private readonly ValidationService _validationService;
        private readonly IWacService _wacService;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

        public VaultService(BaskentEnerjiDbContext context, IMapper mapper, IMemoryCache memoryCache, ValidationService validationService, IWacService wacService)
        {
            _context = context;
            _mapper = mapper;
            _memoryCache = memoryCache;
            _validationService = validationService;
            _wacService = wacService;
        }

        // Satılmamış (elde kalan) döviz stoku, gerçekleşmemiş kâr/zararı erken muhasebeleştirmemek
        // için güncel piyasa kuru yerine ortalama alış maliyetiyle (WAC) değerlenir — gerçek kâr/zarar
        // yalnızca fiili satış anında sisteme işlenir. WAC henüz oluşmamışsa (hiç alış yapılmamış
        // para birimi) bilgi amaçlı olarak güncel piyasa kuruna düşülür.
        private async Task<Dictionary<(Guid VaultId, Guid CurrencyId), decimal>> GetWacsForVaultsAsync(List<Guid> vaultIds)
        {
            var rows = await _context.CurrencyWacs
                .Where(w => vaultIds.Contains(w.VaultId) && w.Quantity > 0)
                .ToListAsync();

            return rows
                .GroupBy(w => (w.VaultId, w.CurrencyId))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(w => w.LastUpdated).First().Wac);
        }

        public async Task<decimal> GetTotalAssetsInBaseCurrencyAsync(Guid? officeId = null)
        {
            var query = _context.VaultBalances
                .Include(vb => vb.Vault)
                .Include(vb => vb.Currency)
                .Where(vb => vb.Vault.IsActive);

            if (officeId.HasValue)
            {
                query = query.Where(vb => vb.Vault.OfficeId == officeId.Value);
            }

            var balances = await query.ToListAsync();

            // Pre-fetch all required exchange rates
            var exchangeRates = await GetExchangeRatesForCurrenciesAsync(balances.Select(b => b.CurrencyId).Distinct());

            decimal totalAssets = 0;
            var baseCurrencyId = await GetBaseCurrencyIdAsync();

            foreach (var balance in balances)
            {
                if (balance.CurrencyId == baseCurrencyId)
                {
                    totalAssets += balance.Balance;
                }
                else
                {
                    decimal rate = 0;
                    if (exchangeRates.TryGetValue(balance.CurrencyId, out var foundRate))
                        rate = foundRate;
                    totalAssets += balance.Balance * rate;
                }
            }

            return totalAssets;
        }

        public async Task<bool> CheckVaultBalanceAsync(Guid vaultId, Guid currencyId, decimal requiredAmount)
        {
            // Use UPDLOCK when called within a transaction to prevent TOCTOU:
            // two concurrent transfers both reading sufficient balance then both deducting
            var balance = await _context.VaultBalances
                .FromSqlRaw("SELECT * FROM VaultBalances WITH (UPDLOCK) WHERE VaultId = {0} AND CurrencyId = {1}", vaultId, currencyId)
                .FirstOrDefaultAsync();

            if (balance == null)
                return false;

            return balance.Balance >= requiredAmount;
        }

        public async Task<decimal> GetLockedBalanceAsync(Guid vaultId, Guid currencyId)
        {
            // CheckVaultBalanceAsync ile aynı UPDLOCK deseni — ama burada satış sonrası yeni miktarı
            // hesaplamak için (WAC güncellemesi) gerçek bakiye değerine ihtiyaç duyan çağıranlar için.
            // Önceden ExchangeTransactionService bu sorguyu UPDLOCK'suz, kendi içinde tekrar yazıyordu.
            var balance = await _context.VaultBalances
                .FromSqlRaw("SELECT * FROM VaultBalances WITH (UPDLOCK) WHERE VaultId = {0} AND CurrencyId = {1}", vaultId, currencyId)
                .FirstOrDefaultAsync();

            return balance?.Balance ?? 0m;
        }

        public async Task<vm_vaultsummary> GetVaultSummaryAsync(Guid vaultId)
        {
            var vault = await _context.Vaults
                .Include(v => v.Office)
                .Include(v => v.Balances)
                    .ThenInclude(b => b.Currency)
                .FirstOrDefaultAsync(v => v.Id == vaultId && v.IsActive);

            if (vault == null)
                return null;

            var summary = new vm_vaultsummary
            {
                VaultId = vault.Id,
                VaultName = vault.Name,
                OfficeId = vault.OfficeId,
                OfficeName = vault.Office.OfficeName,
                IsActive = vault.IsActive,
                ShouldCount = vault.ShouldCount,
                LastCountDate = vault.LastCountDate,
                Balances = new List<vm_vaultbalance>(),
                BalanceHistories = new List<vm_vaultbalancehistory>(),
            };

            // Pre-fetch exchange rates for all currencies
            var currencyIds = vault.Balances.Select(b => b.CurrencyId).Distinct().ToList();
            var exchangeRates = await GetExchangeRatesForCurrenciesAsync(currencyIds);
            var vaultWacs = await _wacService.GetAllWacsForVaultAsync(vaultId);
            var baseCurrencyId = await GetBaseCurrencyIdAsync();

            decimal totalValue = 0;

            foreach (var balance in vault.Balances)
            {
                decimal valueInBase;
                decimal exchangeRate;
                decimal unrealizedProfit = 0;

                if (balance.CurrencyId == baseCurrencyId)
                {
                    valueInBase = balance.Balance;
                    exchangeRate = 1;
                }
                else
                {
                    var hasWac = vaultWacs.TryGetValue(balance.CurrencyId, out var wacRate) && wacRate > 0;
                    var hasMarketRate = exchangeRates.TryGetValue(balance.CurrencyId, out var marketRate);

                    exchangeRate = hasWac ? wacRate : (hasMarketRate ? marketRate : 0);
                    valueInBase = balance.Balance * exchangeRate;

                    // Defter değeri (WAC) sabit kalır; piyasa kuruyla WAC arasındaki fark, henüz
                    // gerçekleşmemiş kâr/zarar olarak AYRICA gösterilir (TMS 21 — kur farkı bilgisi).
                    if (hasWac && hasMarketRate)
                        unrealizedProfit = (marketRate - wacRate) * balance.Balance;
                }

                summary.Balances.Add(new vm_vaultbalance
                {
                    CurrencyId = balance.Currency.Id,
                    CurrencyCode = balance.Currency.CurrencyCode,
                    CurrencyName = balance.Currency.CurrencyName,
                    Balance = balance.Balance,
                    ValueInBaseCurrency = valueInBase,
                    ExchangeRateToBase = exchangeRate,
                    UnrealizedProfit = unrealizedProfit
                });

                totalValue += valueInBase;
            }

            summary.TotalValueInBaseCurrency = totalValue;

            // Order balances by value in base currency (biggest first)
            summary.Balances = summary.Balances.OrderByDescending(b => b.ValueInBaseCurrency).ToList();

            // Load balance histories with proper eager loading to avoid N+1 queries
            var dbHistories = await _context.VaultBalanceHistories
                .AsNoTracking()
                .Where(x => x.VaultId == summary.VaultId && !x.IsDeleted)
                .Include(x => x.Currency)
                .OrderByDescending(x => x.CreatedDate)
                .Take(100) // Limit to last 100 entries for performance
                .ToListAsync();

            // Pre-fetch all users for histories to avoid N+1 queries
            var userIds = dbHistories.Select(h => h.UserId).Distinct().ToList();
            var users = await _context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Firstname + " " + u.Lastname);

            var historyCurrencyIds = dbHistories.Select(h => h.CurrencyId).Distinct().ToList();
            var historyExchangeRates = await GetExchangeRatesForCurrenciesAsync(historyCurrencyIds);

            foreach (var v in dbHistories)
            {
                decimal valueInBase;
                if (v.CurrencyId == baseCurrencyId)
                {
                    valueInBase = v.Balance;
                }
                else
                {
                    decimal rate = 0;
                    if (historyExchangeRates.TryGetValue(v.CurrencyId, out var foundRate))
                        rate = foundRate;
                    valueInBase = v.Balance * rate;
                }

                var balanceHistory = new vm_vaultbalancehistory
                {
                    Balance = v.Balance,
                    CreatedDate = v.CreatedDate,
                    CurrencyCode = v.Currency.CurrencyCode,
                    CurrencyId = v.CurrencyId,
                    CurrencyName = v.Currency.CurrencyName,
                    Description = v.Description,
                    ValueInBaseCurrency = valueInBase,
                    Id = v.Id,
                    TransactionType = v.TransactionType,
                    User = v.UserId.HasValue && users.TryGetValue(v.UserId.Value, out var userName) ? userName : "Unknown User"
                };
                summary.BalanceHistories.Add(balanceHistory);
            }

            return summary;
        }
        public async Task<List<vm_vaultsummary>> GetAllVaultSummariesAsync()
        {
            // Use caching for frequently accessed data
            var cacheKey = $"VaultSummaries_{_validationService.GetUserID()}";
           // if (_memoryCache.TryGetValue(cacheKey, out List<vm_vaultsummary> cachedSummaries))
           // {
           //     return cachedSummaries;
          //  }

            var vaults = new List<Vault>();

            if (!await _validationService.IsAdminAsync())
            {
                var userIdStr = _validationService.GetUserID();
                if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var parsedUserId))
                {
                    return new List<vm_vaultsummary>();
                }

                var userOfficeIds = await _context.User_Offices
                    .AsNoTracking()
                    .Where(x => x.UserId == parsedUserId)
                    .Select(x => x.OfficeId)
                    .ToListAsync();

                if (!userOfficeIds.Any())
                {
                    return new List<vm_vaultsummary>();
                }

                vaults = await _context.Vaults
                   .AsNoTracking()
                   .Include(v => v.Office)
                   .Include(v => v.Balances)
                       .ThenInclude(b => b.Currency)
                   .Where(v => v.IsActive && userOfficeIds.Contains(v.OfficeId))
                   .ToListAsync();
            }
            else
            {
                vaults = await _context.Vaults
                    .AsNoTracking()
                    .Include(v => v.Office)
                    .Include(v => v.Balances)
                        .ThenInclude(b => b.Currency)
                    .Where(v => v.IsActive)
                    .ToListAsync();
            }

            // Get all unique currency IDs from all vaults
            var allCurrencyIds = vaults
                .SelectMany(v => v.Balances.Select(b => b.CurrencyId))
                .Distinct()
                .ToList();

            // Pre-fetch all exchange rates at once
            var exchangeRates = await GetExchangeRatesForCurrenciesAsync(allCurrencyIds);
            var allVaultWacs = await GetWacsForVaultsAsync(vaults.Select(v => v.Id).ToList());
            var baseCurrencyId = await GetBaseCurrencyIdAsync();

            var summaries = new List<vm_vaultsummary>();

            foreach (var vault in vaults)
            {
                var summary = new vm_vaultsummary
                {
                    Id = vault.Id,
                    VaultId = vault.Id,
                    VaultName = vault.Name,
                    OfficeId = vault.OfficeId,
                    OfficeName = vault.Office.OfficeName,
                    Balances = new List<vm_vaultbalance>(),
                    BalanceHistories = new List<vm_vaultbalancehistory>(),
                    IsActive = vault.IsActive,
                    ShouldCount = vault.ShouldCount,
                    LastCountDate = vault.LastCountDate
                };

                // Skip loading balance histories in list view for performance
                // They will be loaded only in detail view (GetVaultSummaryAsync)
                summary.BalanceHistories = new List<vm_vaultbalancehistory>();
                decimal totalValue = 0;

                foreach (var balance in vault.Balances)
                {
                    decimal valueInBase;
                    decimal exchangeRate;
                    decimal unrealizedProfit = 0;

                    if (balance.CurrencyId == baseCurrencyId)
                    {
                        valueInBase = balance.Balance;
                        exchangeRate = 1;
                    }
                    else
                    {
                        var hasWac = allVaultWacs.TryGetValue((vault.Id, balance.CurrencyId), out var wacRate) && wacRate > 0;
                        var hasMarketRate = exchangeRates.TryGetValue(balance.CurrencyId, out var marketRate);

                        exchangeRate = hasWac ? wacRate : (hasMarketRate ? marketRate : 0);
                        valueInBase = balance.Balance * exchangeRate;

                        if (hasWac && hasMarketRate)
                            unrealizedProfit = (marketRate - wacRate) * balance.Balance;
                    }

                    summary.Balances.Add(new vm_vaultbalance
                    {
                        CurrencyId = balance.Currency.Id,
                        CurrencyCode = balance.Currency.CurrencyCode,
                        CurrencyName = balance.Currency.CurrencyName,
                        Balance = balance.Balance,
                        ExchangeRateToBase = exchangeRate,
                        ValueInBaseCurrency = valueInBase,
                        UnrealizedProfit = unrealizedProfit
                    });


                    totalValue += valueInBase;


                }

                summary.TotalValueInBaseCurrency = totalValue;

                // Order balances by value in base currency (biggest first)
                summary.Balances = summary.Balances.OrderByDescending(b => b.ValueInBaseCurrency).ToList();

                summaries.Add(summary);
            }

            // Cache the result
           // _memoryCache.Set(cacheKey, summaries, CacheDuration);
            return summaries;
        }

        public async Task<List<vm_officesummary>> GetOfficeSummariesAsync()
        {
            var isAdmin = await _validationService.IsAdminAsync();
            var cacheKey = isAdmin ? "OfficeSummaries_All" : $"OfficeSummaries_{_validationService.GetUserID()}";
            if (_memoryCache.TryGetValue(cacheKey, out List<vm_officesummary> cachedOfficeSummaries))
            {
                return cachedOfficeSummaries;
            }

            var officeQuery = _context.Offices
                .AsNoTracking()
                .Include(o => o.Vaults)
                    .ThenInclude(v => v.Balances)
                        .ThenInclude(b => b.Currency)
                .Where(o => o.IsActive);

            if (!isAdmin)
            {
                var userIdStr = _validationService.GetUserID();
                if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var parsedUserId))
                {
                    var userOfficeIds = await _context.User_Offices
                        .AsNoTracking()
                        .Where(x => x.UserId == parsedUserId)
                        .Select(x => x.OfficeId)
                        .ToListAsync();
                    officeQuery = officeQuery.Where(o => userOfficeIds.Contains(o.Id));
                }
                else
                {
                    return new List<vm_officesummary>();
                }
            }

            var offices = await officeQuery
                .OrderBy(o => o.OfficeType)
                .ThenBy(o => o.OfficeName)
                .AsSplitQuery()
                .ToListAsync();

            var userCounts = await _context.User_Offices
                .AsNoTracking()
                .Where(uo => uo.IsActive)
                .GroupBy(uo => uo.OfficeId)
                .Select(g => new { OfficeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.OfficeId, x => x.Count);

            // Pre-fetch all currencies and their exchange rates
            var allCurrencyCodes = offices
                .SelectMany(o => o.Vaults.Where(v => v.IsActive))
                .SelectMany(v => v.Balances)
                .Select(b => b.Currency.CurrencyCode)
                .Distinct()
                .ToList();

            var currencies = await _context.Currencies
                .Where(c => allCurrencyCodes.Contains(c.CurrencyCode))
                .ToDictionaryAsync(c => c.CurrencyCode, c => c.Id);

            var exchangeRates = await GetExchangeRatesForCurrenciesAsync(currencies.Values);
            var baseCurrencyId = await GetBaseCurrencyIdAsync();

            var netDebtByOfficeId = await CalculateBranchNetDebtToMerkezAsync(offices, baseCurrencyId);

            var summaries = new List<vm_officesummary>();

            foreach (var office in offices)
            {
                var summary = new vm_officesummary
                {
                    OfficeId = office.Id,
                    OfficeName = office.OfficeName,
                    OfficeType = (int)office.OfficeType,
                    ParentOfficeId = office.ParentOfficeId,
                    IsActive = office.IsActive,
                    VaultCount = office.Vaults.Count(v => v.IsActive),
                    UserCount = userCounts.GetValueOrDefault(office.Id, 0),
                    TotalBalancesByCurrency = new Dictionary<string, decimal>()
                };

                // Aggregate balances by currency
                var balancesByCurrency = office.Vaults
                    .Where(v => v.IsActive)
                    .SelectMany(v => v.Balances)
                    .GroupBy(b => b.Currency.CurrencyCode)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(b => b.Balance)
                    );

                summary.TotalBalancesByCurrency = balancesByCurrency;

                // Calculate total value in base currency
                decimal totalValue = 0;
                foreach (var kvp in balancesByCurrency)
                {
                    if (currencies.TryGetValue(kvp.Key, out var currencyId))
                    {
                        if (currencyId == baseCurrencyId)
                        {
                            totalValue += kvp.Value;
                        }
                        else
                        {
                            decimal rate = 0;
                            if (exchangeRates.TryGetValue(currencyId, out var foundRate))
                                rate = foundRate;
                            totalValue += kvp.Value * rate;
                        }
                    }
                }

                summary.TotalValueInBaseCurrency = totalValue;

                // Calculate P&L
                summary.DailyProfitLoss = await CalculateProfitLossAsync(
                    office.Id,
                    DateTime.Today,
                    DateTime.Today.AddDays(1).AddSeconds(-1)
                );

                summary.MonthlyProfitLoss = await CalculateProfitLossAsync(
                    office.Id,
                    new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                    DateTime.Today.AddDays(1).AddSeconds(-1)
                );

                summary.NetDebtToMerkez = netDebtByOfficeId.GetValueOrDefault(office.Id, 0m);

                summaries.Add(summary);
            }

            // Cache the result
            _memoryCache.Set(cacheKey, summaries, CacheDuration);
            return summaries;
        }

        // Şubelerin Merkez'e olan net borcunu, tamamlanmış OfficeTransfer kayıtlarından türetir.
        // Merkez → Şube transferi sermaye avansı (borcu artırır), Şube → Merkez transferi geri ödeme/kâr havalesi (borcu azaltır).
        // Kalıcı bir borç tablosu yok — bu değer her çağrıda mevcut transfer geçmişinden hesaplanır.
        private async Task<Dictionary<Guid, decimal>> CalculateBranchNetDebtToMerkezAsync(List<BaskentEnerji.Entity.Entities.ExchangeOffice.Office.Office> offices, Guid baseCurrencyId)
        {
            var netDebtByOfficeId = new Dictionary<Guid, decimal>();

            var merkezOffice = offices.FirstOrDefault(o => o.OfficeType == OfficeType.Merkez);
            var subeOffices = offices.Where(o => o.OfficeType == OfficeType.Sube).ToList();
            if (merkezOffice == null || subeOffices.Count == 0)
                return netDebtByOfficeId;

            var merkezVaultIds = merkezOffice.Vaults.Select(v => v.Id).ToHashSet();
            var subeVaultToOfficeId = subeOffices
                .SelectMany(o => o.Vaults.Select(v => new { v.Id, OfficeId = o.Id }))
                .ToDictionary(x => x.Id, x => x.OfficeId);

            if (subeVaultToOfficeId.Count == 0)
                return netDebtByOfficeId;

            var subeVaultIds = subeVaultToOfficeId.Keys.ToList();

            var transfers = await _context.OfficeTransfers
                .AsNoTracking()
                .Where(t => t.Status == TransferStatus.Completed &&
                    ((merkezVaultIds.Contains(t.SourceVaultId) && subeVaultIds.Contains(t.TargetVaultId)) ||
                     (subeVaultIds.Contains(t.SourceVaultId) && merkezVaultIds.Contains(t.TargetVaultId))))
                .ToListAsync();

            if (transfers.Count == 0)
                return netDebtByOfficeId;

            // Her transferin kendi tarihindeki kuru kullanılmalı — güncel kur değil,
            // yoksa geçmiş bir borcun TRY karşılığı gün geçtikçe kayar. Tüm kur geçmişini
            // tek seferde çekip bellekte (transfer tarihi, kur) eşlemesi yapıyoruz.
            var transferCurrencyIds = transfers.Select(t => t.CurrencyId).Distinct().ToList();
            var rateHistory = await _context.ExchangeRates
                .AsNoTracking()
                .Where(r => transferCurrencyIds.Contains(r.SourceCurrencyId) && r.TargetCurrencyId == baseCurrencyId)
                .OrderBy(r => r.EffectiveFrom)
                .Select(r => new { r.SourceCurrencyId, r.EffectiveFrom, r.BuyRate })
                .ToListAsync();

            var ratesByCurrency = rateHistory
                .GroupBy(r => r.SourceCurrencyId)
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.EffectiveFrom).ToList());

            decimal GetRateAtDate(Guid currencyId, DateTime asOf)
            {
                if (!ratesByCurrency.TryGetValue(currencyId, out var history) || history.Count == 0)
                    return 0m;
                var applicable = history.LastOrDefault(r => r.EffectiveFrom <= asOf);
                return (applicable ?? history[0]).BuyRate;
            }

            foreach (var t in transfers)
            {
                decimal valueInBase;
                if (t.CurrencyId == baseCurrencyId)
                {
                    valueInBase = t.Amount;
                }
                else
                {
                    var rate = GetRateAtDate(t.CurrencyId, t.CreatedDate);
                    valueInBase = t.Amount * rate;
                }

                if (merkezVaultIds.Contains(t.SourceVaultId))
                {
                    var subeOfficeId = subeVaultToOfficeId[t.TargetVaultId];
                    netDebtByOfficeId[subeOfficeId] = netDebtByOfficeId.GetValueOrDefault(subeOfficeId, 0m) + valueInBase;
                }
                else
                {
                    var subeOfficeId = subeVaultToOfficeId[t.SourceVaultId];
                    netDebtByOfficeId[subeOfficeId] = netDebtByOfficeId.GetValueOrDefault(subeOfficeId, 0m) - valueInBase;
                }
            }

            return netDebtByOfficeId;
        }

        public async Task<decimal> CalculateProfitLossAsync(Guid officeId, DateTime startDate, DateTime endDate)
        {
            return await _context.Transactions
                .AsNoTracking()
                .Where(t => t.Vault.OfficeId == officeId &&
                           t.TransactionDate >= startDate &&
                           t.TransactionDate <= endDate &&
                           t.Status == TransactionStatus.Completed &&
                           (t.Type == TransactionType.Exchange || t.Type == TransactionType.Buy))
                .SumAsync(t => t.Profit);
        }

        private async Task<decimal> ConvertToBaseCurrencyAsync(Guid currencyId, decimal amount)
        {

            var baseCurrency = await _context.Currencies.FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
            if (baseCurrency == null || currencyId == baseCurrency.Id)
                return amount;

            var rate = await _context.ExchangeRates
                .Where(r => r.SourceCurrencyId == currencyId &&
                           r.TargetCurrencyId == baseCurrency.Id &&
                           r.IsActive)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync();

            return rate != null ? amount * rate.BuyRate : 0;
        }

        private async Task<decimal> GetExchangeRateToBaseAsync(Guid currencyId)
        {
            var baseCurrency = await _context.Currencies.FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
            if (baseCurrency == null || currencyId == baseCurrency.Id)
                return 1;

            var rate = await _context.ExchangeRates
                .Where(r => r.SourceCurrencyId == currencyId &&
                           r.TargetCurrencyId == baseCurrency.Id &&
                           r.IsActive)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync();

            return rate?.BuyRate ?? 0;
        }

        public async Task UpdateVaultBalanceAsync(rm_updatevaultbalance data)
        {
            // Get vault for later cache invalidation
            var vault = await _context.Vaults.FirstOrDefaultAsync(v => v.Id == data.vaultId);
            if (vault != null)
                await _validationService.EnsureNotViewerAsync(vault.OfficeId);

            // Use UPDLOCK to prevent concurrent read-modify-write race conditions
            // When called within a transaction (exchange, transfer), this holds a write lock
            // until the transaction commits, preventing lost updates
            var balance = await _context.VaultBalances
                .FromSqlRaw("SELECT * FROM VaultBalances WITH (UPDLOCK) WHERE VaultId = {0} AND CurrencyId = {1}", data.vaultId, data.currencyId)
                .Include(x => x.Currency).FirstOrDefaultAsync();

            decimal currentBalance = 0;

            if (balance == null)
            {
                balance = new VaultBalance
                {
                    Id = Guid.NewGuid(),
                    VaultId = data.vaultId,
                    CurrencyId = data.currencyId,
                    Balance = 0,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdated = DateTime.Now,
                };
                _context.VaultBalances.Add(balance);

            }
            else currentBalance = balance.Balance;

            if (data.isEntireBalance) balance.Balance = data.amount;
            else balance.Balance += data.amount;

            balance.LastUpdated = DateTime.UtcNow;
            TransactionType iType;

            if (data.TransactionType != TransactionType.Exchange && data.TransactionType != TransactionType.Buy && data.TransactionType != TransactionType.Transfer)
            {
                if (data.isEntireBalance)
                {
                    iType = TransactionType.Adjustment;
                    data.description = $"{balance.Currency.CurrencyName} elle düzeltildi {currentBalance:0.00} -> {balance.Balance:0.00} ({balance.Balance - currentBalance:0.00}) ";

                    // Manuel "tam bakiye düzeltmesi" bir fiziksel sayım düzeltmesidir — kasada
                    // zaten olduğu varsayılan bir miktarı yansıtır, yeni bir "alış" değildir. Bu
                    // yüzden WAC (birim maliyet) DEĞİŞTİRİLMEZ, sadece miktar senkronize edilir —
                    // aksi halde bu düzeltmeyle eklenen kısmın maliyeti hiç kayda girmez ve
                    // sonraki satışların kâr hesabı gerçek maliyeti yansıtmaz hale gelirdi.
                    if (balance.Currency.CurrencyCode != "TRY")
                    {
                        await _wacService.AdjustWacQuantityAsync(data.vaultId, data.currencyId, balance.Balance, WacAdjustReason.ManualAdjustment);
                    }
                }
                else if (data.amount > 0)
                {
                    iType = TransactionType.Deposit;
                    if (string.IsNullOrWhiteSpace(data.description))
                        throw new ApiException(System.Net.HttpStatusCode.BadRequest, "TL/kasa girişlerinde açıklama zorunludur.");
                }
                else
                {
                    iType = TransactionType.Withdrawal;
                    if (string.IsNullOrWhiteSpace(data.description))
                        throw new ApiException(System.Net.HttpStatusCode.BadRequest, "TL/kasa çıkışlarında açıklama zorunludur.");
                    // Manuel kasa çıkışı, kasada olmayan parayı "yok yere" negatif bakiyeye
                    // düşürebiliyordu — frontend'de eskiden hiç kontrol yoktu, backend de
                    // balance.Balance += data.amount'ı (satır 646) hiç sınırlamıyordu.
                    if (balance.Balance < 0)
                        throw new ApiException(System.Net.HttpStatusCode.BadRequest,
                            $"Yetersiz bakiye: kasada {currentBalance:F2} {balance.Currency?.CurrencyCode} var, {Math.Abs(data.amount):F2} çekilmeye çalışıldı.");
                }
            }
            else
            {
                iType = data.TransactionType ?? TransactionType.Exchange;
            }


            VaultBalanceHistory nHistory = new VaultBalanceHistory
            {
                Balance = data.amount,
                CreatedDate = DateTime.UtcNow,
                Currency = balance.Currency,
                CurrencyId = balance.CurrencyId,
                Description = data.description,
                VaultId = data.vaultId,
                Vault = balance.Vault,
                Id = Guid.NewGuid(),
                TransactionType = iType,
                UserId = Guid.Parse(_validationService.GetUserID()),
                IsAbsoluteBalance = data.isEntireBalance
            };

            //if (!data.isEntireBalance) _context.VaultBalanceHistories.Add(nHistory);
            _context.VaultBalanceHistories.Add(nHistory);

            await _context.SaveChangesAsync();

            // Clear vault caches AFTER commit
            ClearVaultCaches();

            // Invalidate Z-report cache AFTER saving changes
            if (vault != null)
            {
                InvalidateZReportCache(vault.OfficeId, DateTime.Today);
                InvalidateZReportCache(vault.OfficeId, nHistory.CreatedDate.Date);
                InvalidateZReportCache(vault.OfficeId, DateTime.Now.Date);
            }
        }

        // Bir kasa hareketi (VaultBalanceHistory) kaydını geri alınamaz şekilde
        // silmek yerine "iptal" (soft-delete) eder ve o vault+para birimi için
        // silinmemiş tüm geçmişi yeniden oynatarak (replay) güncel bakiyeyi
        // yeniden hesaplar — kaydın sırası/tipi ne olursa olsun her zaman doğru sonucu garanti eder.
        // Birleştirilmiş (Alış/Satış) kasa hareketi satırlarının iki bacağını (alınan+verilen)
        // aynı anda iptal eder. Tek tek void çağrısı yapılsaydı, ikinci bacak API/ağ hatasıyla
        // başarısız olduğunda ilk bacak zaten iptal edilmiş kalır ve tutarsız bir durum oluşurdu —
        // bu yüzden tek bir DB transaction'ı içinde sarmalanıyor: biri başarısız olursa hiçbiri
        // kalıcı olmaz.
        public async Task VoidVaultBalanceHistoriesAsync(List<Guid> historyIds, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var id in historyIds)
                {
                    await VoidVaultBalanceHistoryAsync(id, reason);
                }
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task VoidVaultBalanceHistoryAsync(Guid historyId, string reason)
        {
            if (!await _validationService.IsOwnerAsync())
                throw new ApiException(System.Net.HttpStatusCode.Forbidden, "Kasa hareketi iptali için Owner yetkisi gereklidir.");

            var history = await _context.VaultBalanceHistories.FindAsync(historyId);
            if (history == null)
                throw new ApiException(System.Net.HttpStatusCode.NotFound, "Kasa hareketi bulunamadı.");
            if (history.IsDeleted)
                throw new ApiException(System.Net.HttpStatusCode.BadRequest, "Bu kasa hareketi zaten iptal edilmiş.");

            var currentUserId = Guid.Parse(_validationService.GetUserID());

            history.IsDeleted = true;
            history.DeletedReason = reason;
            history.DeletedByUserId = currentUserId;

            var remainingHistory = await _context.VaultBalanceHistories
                .Where(h => h.VaultId == history.VaultId && h.CurrencyId == history.CurrencyId && !h.IsDeleted && h.Id != historyId)
                .OrderBy(h => h.CreatedDate)
                .ToListAsync();

            decimal replayedBalance = 0;
            foreach (var row in remainingHistory)
            {
                // TransactionType.Adjustment iki farklı şeyi ifade edebilir: "elle düzeltildi" (Balance =
                // mutlak yeni bakiye) veya "kasa sayımı"/"gün kapanışı sayım farkı" (Balance = sadece fark).
                // Sadece IsAbsoluteBalance=true olanlar mutlak atama, geri kalan HER ŞEY (bu tür Adjustment
                // kayıtları dahil) kümülatif toplamaya dahil edilmeli — aksi halde bir delta kaydı yanlışlıkla
                // bakiyeyi o küçük delta değerine sıfırlar.
                if (row.IsAbsoluteBalance)
                    replayedBalance = row.Balance;
                else
                    replayedBalance += row.Balance;
            }

            var balance = await _context.VaultBalances
                .FirstOrDefaultAsync(vb => vb.VaultId == history.VaultId && vb.CurrencyId == history.CurrencyId);
            if (balance != null)
            {
                balance.Balance = replayedBalance;
                balance.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            ClearVaultCaches();

            var vaultForCache = await _context.Vaults.FindAsync(history.VaultId);
            if (vaultForCache != null)
            {
                InvalidateZReportCache(vaultForCache.OfficeId, DateTime.Today);
                InvalidateZReportCache(vaultForCache.OfficeId, history.CreatedDate.Date);
                InvalidateZReportCache(vaultForCache.OfficeId, DateTime.Now.Date);
            }
        }

        public async Task SaveVault(rm_savevault data)
        {
            await _validationService.EnsureNotViewerAsync(data.OfficeId);

            var dbVault = await _context.Vaults.FindAsync(data.Id);

            if (dbVault != null)
            {
                // Update existing entity - map directly to the tracked entity
                _mapper.Map(data, dbVault);
            }
            else
            {
                // Create new entity
                var newVault = _mapper.Map<Vault>(data);
                await _context.Vaults.AddAsync(newVault);
            
                // Get base currency (TRY)
                var baseCurrency = await _context.Currencies.FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");

                // Get the last closed vault for this office to inherit TRY balance
                var previousVault = await _context.Vaults
                    .Where(v => v.OfficeId == newVault.OfficeId &&
                               v.Id != newVault.Id &&
                               !v.IsActive &&
                               v.ClosedDate != null)
                    .OrderByDescending(v => v.ClosedDate)
                    .FirstOrDefaultAsync();

                var dbCurrencies = _context.Currencies.ToList();
             
                // Initialize all currencies with zero balance
                foreach (var currency in dbCurrencies)
                {
                    decimal initialAmount = 0;
                    string description = "";

                    // Only carry over TRY balance from previous vault's closing balance
                    if (baseCurrency != null && currency.Id == baseCurrency.Id && previousVault != null)
                    {
                        initialAmount = previousVault.ClosingBalance;
                        if (initialAmount > 0)
                        {
                            description = $"Initial balance from previous vault (closed on {previousVault.ClosedDate:yyyy-MM-dd})";
                        }
                    }

                    var newBalance = new rm_updatevaultbalance
                    {
                        amount = initialAmount,
                        currencyId = currency.Id,
                        description = description,
                        isEntireBalance = true,
                        vaultId = newVault.Id,
                    };
                    await UpdateVaultBalanceAsync(newBalance);
                }
            }

            await _context.SaveChangesAsync();

            // Clear vault caches AFTER commit
            ClearVaultCaches();

            // Invalidate Z-report cache AFTER saving changes
            InvalidateZReportCache(data.OfficeId, DateTime.Today);
            InvalidateZReportCache(data.OfficeId, DateTime.Now.Date);
        }

        public async Task RemoveVault(Guid id)
        {
            var dbVault = await _context.Vaults.FindAsync(id);
            var officeId = dbVault?.OfficeId;
            if (officeId.HasValue)
                await _validationService.EnsureNotViewerAsync(officeId.Value);

            // ExpensePayments -> Vault FK artık Restrict (denetim izini korumak için) — bu yüzden
            // ilişkili gider ödemesi olan bir kasa silinmeye çalışılırsa burada açıkça engellenir,
            // aksi halde ham bir FK constraint hatası kullanıcıya 500 olarak yansırdı.
            var hasExpensePayments = await _context.ExpensePayments.AnyAsync(ep => ep.VaultId == id);
            if (hasExpensePayments)
                throw new InvalidOperationException("Bu kasaya bağlı gider ödemeleri bulunduğu için kasa silinemez.");

            // Bakiyesi sıfır olmayan bir kasa silinirse, o para sessizce "yok olur" — kasa ve
            // ilişkili işlemler kayıttan kalkar ama gerçekte kimseye ait olmayan bir bakiye ortada
            // kalmış olur (denetim izi kopar). Silmeden önce tüm para birimlerinde bakiyenin sıfır
            // olduğunu doğruluyoruz.
            var nonZeroBalance = await _context.VaultBalances
                .Include(vb => vb.Currency)
                .Where(vb => vb.VaultId == id && vb.Balance != 0)
                .FirstOrDefaultAsync();
            if (nonZeroBalance != null)
                throw new InvalidOperationException(
                    $"Bu kasada {nonZeroBalance.Currency?.CurrencyCode} cinsinden {nonZeroBalance.Balance:F2} bakiye bulunduğu için kasa silinemez. Önce bakiyeyi sıfırlayın.");

            var dbTransactions = _context.Transactions.Where(x => x.VaultId == dbVault.Id);
            _context.Transactions.RemoveRange(dbTransactions);
            await _context.SaveChangesAsync();
            _context.Vaults.Remove(dbVault);
            await _context.SaveChangesAsync();

            // Clear vault caches AFTER commit
            ClearVaultCaches();

            // Invalidate Z-report cache AFTER removing
            if (officeId != null)
            {
                InvalidateZReportCache(officeId.Value, DateTime.Today);
                InvalidateZReportCache(officeId.Value, DateTime.Now.Date);
            }

        }

        private async Task<Guid> GetBaseCurrencyIdAsync()
        {
            const string cacheKey = "BaseCurrencyId_TRY";

            if (!_memoryCache.TryGetValue(cacheKey, out Guid baseCurrencyId))
            {
                var baseCurrency = await _context.Currencies.FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
                if (baseCurrency != null)
                {
                    baseCurrencyId = baseCurrency.Id;
                    _memoryCache.Set(cacheKey, baseCurrencyId, CacheDuration);
                }
                else
                {
                    baseCurrencyId = Guid.Empty;
                }
            }

            return baseCurrencyId;
        }

        private async Task<Dictionary<Guid, decimal>> GetExchangeRatesForCurrenciesAsync(IEnumerable<Guid> currencyIds)
        {
            var baseCurrencyId = await GetBaseCurrencyIdAsync();
            var result = new Dictionary<Guid, decimal>();
            var uncachedCurrencyIds = new List<Guid>();

            // Check cache for each currency
            foreach (var currencyId in currencyIds)
            {
                var cacheKey = $"ExchangeRate_{currencyId}_{baseCurrencyId}";
                if (_memoryCache.TryGetValue(cacheKey, out decimal cachedRate))
                {
                    result[currencyId] = cachedRate;
                }
                else
                {
                    uncachedCurrencyIds.Add(currencyId);
                }
            }

            // Fetch uncached rates from database
            if (uncachedCurrencyIds.Any())
            {
                var rates = await _context.ExchangeRates
                    .Where(r => uncachedCurrencyIds.Contains(r.SourceCurrencyId) &&
                               r.TargetCurrencyId == baseCurrencyId &&
                               r.IsActive)
                    .GroupBy(r => r.SourceCurrencyId)
                    .Select(g => g.OrderByDescending(r => r.EffectiveFrom).FirstOrDefault())
                    .ToListAsync();

                // Add to cache and result
                foreach (var rate in rates)
                {
                    if (rate != null)
                    {
                        var cacheKey = $"ExchangeRate_{rate.SourceCurrencyId}_{baseCurrencyId}";
                        _memoryCache.Set(cacheKey, rate.BuyRate, CacheDuration);
                        result[rate.SourceCurrencyId] = rate.BuyRate;
                    }
                }
            }

            return result;
        }

        public void ClearVaultCaches()
        {
            // Remove all vault-related cache entries
            var keysToRemove = new List<string>();

            // This is a simple approach - in production, consider using cache tags or patterns
            keysToRemove.Add($"VaultSummaries_{_validationService.GetUserID()}");
            keysToRemove.Add("OfficeSummaries");

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
            }
        }

        public async Task<object> GetVaultBalanceHistoriesByOfficeAsync(Guid officeId, DateTime? date = null)
        {
            // Build query - EXPLICITLY EXCLUDE DELETED AND GHOST RECORDS
            var query = _context.VaultBalanceHistories
                .AsNoTracking()
                .Include(vh => vh.Vault)
                .Include(vh => vh.Currency)
                .Where(vh => vh.Vault.OfficeId == officeId &&
                             vh.Vault.IsActive &&
                             !vh.IsDeleted &&
                             !vh.IsGhost);

            // Apply date filter if provided  (inclusive start, exclusive end)
            if (date.HasValue)
            {
                var startDate = date.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(vh => vh.CreatedDate >= startDate && vh.CreatedDate < endDate);
            }

            // Order by date descending (newest first), limit to prevent massive loads
            var histories = await query
                .OrderByDescending(vh => vh.CreatedDate)
                .Take(500)
                .ToListAsync();

            // Get user names
            var userIds = histories.Where(h => h.UserId.HasValue).Select(h => h.UserId.Value).Distinct().ToList();
            var users = await _context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => $"{u.Firstname} {u.Lastname}");

            // Map to response model
            var result = histories.Select(vh => new
            {
                Id = vh.Id,
                VaultId = vh.VaultId,
                VaultName = vh.Vault.Name,
                CurrencyId = vh.CurrencyId,
                CurrencyCode = vh.Currency.CurrencyCode,
                CurrencyName = vh.Currency.CurrencyName,
                Balance = vh.Balance,
                Description = vh.Description,
                TransactionType = vh.TransactionType,
                TransactionTypeName = vh.TransactionType.ToString(),
                CreatedDate = vh.CreatedDate,
                UserId = vh.UserId,
                UserName = vh.UserId.HasValue && users.ContainsKey(vh.UserId.Value) ? users[vh.UserId.Value] : "System",

                // Flags + absolute
                IsDebit = vh.Balance < 0,   // giden
                IsCredit = vh.Balance > 0,  // gelen
                AbsoluteAmount = Math.Abs(vh.Balance)
            }).ToList();

            // Local helper: build summary object with desired math (gelen - giden)
            object BuildSummary(object extra)
            {
                var currencyBreakdown = result
                    .GroupBy(r => r.CurrencyCode)
                    .Select(g =>
                    {
                        // gidenleri pozitif topluyoruz
                        decimal totalDebits = g.Where(x => x.Balance < 0).Sum(x => Math.Abs(x.Balance));
                        // gelenler zaten pozitif
                        decimal totalCredits = g.Where(x => x.Balance > 0).Sum(x => x.Balance);
                        decimal net = totalCredits - totalDebits; // gelen - giden

                        return new
                        {
                            currencyCode = g.Key,
                            totalDebits,      // pozitif giden
                            totalCredits,     // pozitif gelen
                            netBalance = net, // gelen - giden
                            transactionCount = g.Count()
                        };
                    })
                    .ToList();

                return new
                {
                    summary = new
                    {
                        totalRecords = result.Count,
                        officeId = officeId,
                        vaultCount = result.Select(r => r.VaultId).Distinct().Count(),
                        currencyBreakdown
                    },
                    data = result,
                    extra
                };
            }

            if (!date.HasValue)
            {
                return BuildSummary(new { dateRange = "All Time" });
            }
            else
            {
                return BuildSummary(new { date = date.Value.Date });
            }
        }

        public async Task<object> GetVaultBalanceHistoriesByVaultAsync(Guid vaultId, DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
        {
            // Build query - EXPLICITLY EXCLUDE DELETED AND GHOST RECORDS
            var query = _context.VaultBalanceHistories
                .AsNoTracking()
                .Include(vh => vh.Currency)
                .Include(vh => vh.Vault)
                .Where(vh => vh.VaultId == vaultId &&
                            vh.IsDeleted == false &&
                            vh.IsGhost == false); // Explicit false checks

            // Apply date filters
            if (startDate.HasValue)
                query = query.Where(vh => vh.CreatedDate >= startDate.Value.Date);

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1);
                query = query.Where(vh => vh.CreatedDate < endOfDay);
            }

            // Order and limit
            var histories = await query
                .OrderByDescending(vh => vh.CreatedDate)
                .Take(limit)
                .ToListAsync();

            // Get user names
            var userIds = histories.Where(h => h.UserId.HasValue).Select(h => h.UserId.Value).Distinct().ToList();
            var users = await _context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => $"{u.Firstname} {u.Lastname}");

            // Map to response model with running balance
            var resultList = new List<dynamic>();
            decimal runningBalance = 0;

            // Process from oldest to newest for running balance calculation
            var orderedHistories = histories.OrderBy(h => h.CreatedDate).ToList();

            foreach (var vh in orderedHistories)
            {
                runningBalance += vh.Balance;
                resultList.Add(new
                {
                    Id = vh.Id,
                    VaultId = vh.VaultId,
                    VaultName = vh.Vault.Name,
                    CurrencyId = vh.CurrencyId,
                    CurrencyCode = vh.Currency.CurrencyCode,
                    CurrencyName = vh.Currency.CurrencyName,
                    Balance = vh.Balance,
                    Description = vh.Description,
                    TransactionType = vh.TransactionType,
                    TransactionTypeName = vh.TransactionType.ToString(),
                    CreatedDate = vh.CreatedDate,
                    UserId = vh.UserId,
                    UserName = vh.UserId.HasValue && users.ContainsKey(vh.UserId.Value) ? users[vh.UserId.Value] : "System",

                    // Additional calculated fields
                    IsDebit = vh.Balance < 0,
                    IsCredit = vh.Balance > 0,
                    AbsoluteAmount = Math.Abs(vh.Balance),
                    RunningBalance = runningBalance
                });
            }

            // Reverse to show newest first
            resultList.Reverse();

            return new
            {
                vaultId = vaultId,
                vaultName = histories.FirstOrDefault()?.Vault?.Name ?? "Unknown",
                totalRecords = resultList.Count,
                dateRange = new
                {
                    from = startDate?.Date ?? histories.LastOrDefault()?.CreatedDate,
                    to = endDate?.Date ?? DateTime.Now
                },
                summary = resultList
                    .GroupBy(r => r.CurrencyCode)
                    .Select(g => new
                    {
                        currencyCode = g.Key,
                        totalDebits = g.Where(x => x.Balance < 0).Sum(x => x.Balance), // Negative values as-is
                        totalCredits = g.Where(x => x.Balance > 0).Sum(x => x.Balance), // Positive values as-is
                        netChange = g.Sum(x => x.Balance),
                        transactionCount = g.Count(),
                        currentBalance = g.First().RunningBalance
                    })
                    .ToList(),
                data = resultList
            };
        }

        private void InvalidateZReportCache(Guid officeId, DateTime date)
        {
            var cacheKey = $"ZReport_Daily_{officeId}_{date:yyyyMMdd}";
            _memoryCache.Remove(cacheKey);

            // "Tüm Şubeler" (officeId=null) birleşik görünümü ayrı bir cache anahtarı kullanıyor —
            // o da temizlenmezse, tek bir ofis için yapılan güncelleme/iptal birleşik raporda görünmez.
            var allOfficesCacheKey = $"ZReport_Daily__{date:yyyyMMdd}";
            _memoryCache.Remove(allOfficesCacheKey);
        }

        public async Task<bool> SubmitVaultCountAsync(rm_vaultcount data)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var vault = await _context.Vaults
                    .Include(v => v.Office)
                    .FirstOrDefaultAsync(v => v.Id == data.VaultId);

                if (vault == null)
                    throw new Exception("Vault not found");

                await _validationService.EnsureNotViewerAsync(vault.OfficeId);

                var vaultCount = new VaultCount
                {
                    Id = Guid.NewGuid(),
                    VaultId = data.VaultId,
                    OfficeId = vault.OfficeId,
                    UserId = Guid.Parse(_validationService.GetUserID()),
                    CountDate = DateTime.Now,
                    IsSystemGenerated = !data.IsManual,
                    HasDiscrepancy = false,
                    CountDetails = new List<VaultCountDetail>()
                };

                var discrepancyDetails = new List<string>();

                foreach (var detail in data.CountDetails)
                {
                    // Get current system balance (tracked entity — sayım farkını düzeltmek için
                    // Balance'ı doğrudan güncelleyeceğiz, salt bir projeksiyon yetmez)
                    var vaultBalance = await _context.VaultBalances
                        .Include(vb => vb.Currency)
                        .FirstOrDefaultAsync(vb => vb.VaultId == data.VaultId && vb.CurrencyId == detail.CurrencyId);
                    var systemBalance = vaultBalance?.Balance ?? 0;

                    var discrepancy = detail.ActualAmount - systemBalance;

                    if (Math.Abs(discrepancy) > FinancialConstants.DiscrepancyThreshold)
                    {
                        vaultCount.HasDiscrepancy = true;
                        var currencyCode = vaultBalance?.Currency?.CurrencyCode ?? (await _context.Currencies
                            .Where(c => c.Id == detail.CurrencyId)
                            .Select(c => c.CurrencyCode)
                            .FirstOrDefaultAsync());

                        discrepancyDetails.Add($"{currencyCode}: Beklenen {systemBalance:F2}, SayÄ±lan {detail.ActualAmount:F2}, Fark {discrepancy:F2}");

                        // Sayım, sistemdeki bakiyeden farklı bir tutar tespit ettiğinde, bu fark
                        // öncesinde sadece VaultCount/VaultCountDetail'da raporlanıyordu ama kasanın
                        // gerçek bakiyesi hiç düzeltilmiyordu — sayım "doğru" tutarı bulduktan sonra
                        // bile sistem yanlış bakiyeyle çalışmaya devam ediyordu. Burada bakiyeyi
                        // sayılan tutara eşitleyip UpdateVaultBalanceAsync'teki manuel düzeltme ile
                        // aynı desende bir Adjustment kaydı oluşturuyoruz.
                        if (vaultBalance != null)
                        {
                            vaultBalance.Balance = detail.ActualAmount;
                            vaultBalance.LastUpdated = DateTime.UtcNow;

                            _context.VaultBalanceHistories.Add(new VaultBalanceHistory
                            {
                                Id = Guid.NewGuid(),
                                Balance = discrepancy,
                                CreatedDate = DateTime.UtcNow,
                                Currency = vaultBalance.Currency,
                                CurrencyId = vaultBalance.CurrencyId,
                                Description = $"Kasa sayımı düzeltmesi: {currencyCode} {systemBalance:F2} -> {detail.ActualAmount:F2} ({discrepancy:F2})",
                                VaultId = data.VaultId,
                                Vault = vault,
                                TransactionType = TransactionType.Adjustment,
                                UserId = Guid.Parse(_validationService.GetUserID())
                            });
                        }
                    }

                    var countDetail = new VaultCountDetail
                    {
                        Id = Guid.NewGuid(),
                        VaultCountId = vaultCount.Id,
                        CurrencyId = detail.CurrencyId,
                        ActualAmount = detail.ActualAmount,
                        SystemAmount = systemBalance,
                        Discrepancy = discrepancy
                    };

                    vaultCount.CountDetails.Add(countDetail);
                }

                if (vaultCount.HasDiscrepancy)
                {
                    vaultCount.DiscrepancyDetails = string.Join("; ", discrepancyDetails);
                }
                else
                {
                    vaultCount.DiscrepancyDetails = string.Empty; // Set empty string when no discrepancy
                }

                // Reset vault shouldCount status
                vault.ShouldCount = false;
                vault.LastCountDate = DateTime.Now;

                _context.VaultCounts.Add(vaultCount);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ClearVaultCaches();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<vm_vaultcount>> GetVaultCountsAsync(Guid vaultId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.VaultCounts
                .Include(vc => vc.Vault)
                .Include(vc => vc.Office)
                .Include(vc => vc.CountDetails)
                    .ThenInclude(cd => cd.Currency)
                .Where(vc => vc.VaultId == vaultId);

            if (startDate.HasValue)
                query = query.Where(vc => vc.CountDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(vc => vc.CountDate <= endDate.Value);

            var counts = await query
                .OrderByDescending(vc => vc.CountDate)
                .ToListAsync();

            // Pre-fetch all users in one query instead of N+1
            var userIds = counts.Select(c => c.UserId).Distinct().ToList();
            var users = await _context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => new { u.Username, Fullname = u.Firstname + " " + u.Lastname });

            var result = new List<vm_vaultcount>();

            foreach (var count in counts)
            {
                users.TryGetValue(count.UserId, out var user);

                var vmCount = new vm_vaultcount
                {
                    Id = count.Id,
                    VaultId = count.VaultId,
                    VaultName = count.Vault.Name,
                    OfficeId = count.OfficeId,
                    OfficeName = count.Office.OfficeName,
                    UserId = count.UserId,
                    Username = user?.Username ?? "Unknown",
                    CountDate = count.CountDate,
                    HasDiscrepancy = count.HasDiscrepancy,
                    DiscrepancyDetails = count.DiscrepancyDetails,
                    IsSystemGenerated = count.IsSystemGenerated,
                    CountDetails = count.CountDetails.Select(cd => new vm_vaultcountdetail
                    {
                        Id = cd.Id,
                        CurrencyId = cd.CurrencyId,
                        CurrencyCode = cd.Currency?.CurrencyCode,
                        CurrencyName = cd.Currency?.CurrencyName,
                        ActualAmount = cd.ActualAmount,
                        SystemAmount = cd.SystemAmount,
                        Discrepancy = cd.Discrepancy
                    }).ToList()
                };

                result.Add(vmCount);
            }

            return result;
        }

        public async Task<bool> ResetVaultCountStatusAsync(Guid vaultId)
        {
            var vault = await _context.Vaults.FindAsync(vaultId);
            if (vault == null)
                return false;

            vault.ShouldCount = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetVaultShouldCountAsync(Guid vaultId, bool shouldCount)
        {
            var vault = await _context.Vaults.FindAsync(vaultId);
            if (vault == null)
                return false;

            vault.ShouldCount = shouldCount;
            if (shouldCount)
                vault.LastCountDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

