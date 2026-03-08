using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MoneyTransferTurkey.Business.Infrastructure.ExchangeOffice.Office;
using MoneyTransferTurkey.Business.Services.Permission;
using MoneyTransferTurkey.Data.Contexts;
using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Office;
using MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Office;
using MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Services.ExchangeOffice.Office
{
    public class VaultService : IVaultService
    {
        private readonly MoneyTransferTurkeyDbContext _context;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _memoryCache;
        private readonly ValidationService _validationService;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

        public VaultService(MoneyTransferTurkeyDbContext context, IMapper mapper, IMemoryCache memoryCache, ValidationService validationService)
        {
            _context = context;
            _mapper = mapper;
            _memoryCache = memoryCache;
            _validationService = validationService;
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
            var balance = await _context.VaultBalances
                .FirstOrDefaultAsync(b => b.VaultId == vaultId && b.CurrencyId == currencyId);

            if (balance == null)
                return false;

            // Check available balance (total balance minus reserved amount)
            //var availableBalance = balance.Balance - balance.ReservedAmount;
            var availableBalance = balance.Balance;
            return availableBalance >= requiredAmount;
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
            var baseCurrencyId = await GetBaseCurrencyIdAsync();

            decimal totalValue = 0;

            foreach (var balance in vault.Balances)
            {
                decimal valueInBase;
                decimal exchangeRate;

                if (balance.CurrencyId == baseCurrencyId)
                {
                    valueInBase = balance.Balance;
                    exchangeRate = 1;
                }
                else
                {
                    if (exchangeRates.TryGetValue(balance.CurrencyId, out var foundRate))
                        exchangeRate = foundRate;
                    else
                        exchangeRate = 0;
                    valueInBase = balance.Balance * exchangeRate;
                }

                summary.Balances.Add(new vm_vaultbalance
                {
                    CurrencyId = balance.Currency.Id,
                    CurrencyCode = balance.Currency.CurrencyCode,
                    CurrencyName = balance.Currency.CurrencyName,
                    Balance = balance.Balance,
                    ValueInBaseCurrency = valueInBase,
                    ExchangeRateToBase = exchangeRate
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
                var dbUserOffice = await _context.User_Offices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.UserId == Guid.Parse(_validationService.GetUserID()));

                vaults = await _context.Vaults
                   .AsNoTracking()
                   .Include(v => v.Office)
                   .Include(v => v.Balances)
                       .ThenInclude(b => b.Currency)
                   .Where(v => v.IsActive && v.OfficeId == dbUserOffice.OfficeId)
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
            var baseCurrencyId = await GetBaseCurrencyIdAsync();

            var summaries = new List<vm_vaultsummary>();

            foreach (var vault in vaults)
            {
                var summary = new vm_vaultsummary
                {
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

                    if (balance.CurrencyId == baseCurrencyId)
                    {
                        valueInBase = balance.Balance;
                        exchangeRate = 1;
                    }
                    else
                    {
                        exchangeRate = exchangeRates.GetValueOrDefault(balance.CurrencyId, 0);
                        valueInBase = balance.Balance * exchangeRate;
                    }

                    summary.Balances.Add(new vm_vaultbalance
                    {
                        CurrencyId = balance.Currency.Id,
                        CurrencyCode = balance.Currency.CurrencyCode,
                        CurrencyName = balance.Currency.CurrencyName,
                        Balance = balance.Balance,
                        ExchangeRateToBase = exchangeRate,
                        ValueInBaseCurrency = valueInBase,
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
            // Use caching for frequently accessed data
            var cacheKey = "OfficeSummaries";
            if (_memoryCache.TryGetValue(cacheKey, out List<vm_officesummary> cachedOfficeSummaries))
            {
                return cachedOfficeSummaries;
            }

            var offices = await _context.Offices
                .AsNoTracking()
                .Include(o => o.Vaults)
                    .ThenInclude(v => v.Balances)
                        .ThenInclude(b => b.Currency)
                .Where(o => o.IsActive)
                .OrderBy(o => o.OfficeName)
                .AsSplitQuery()
                .ToListAsync();

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

            var summaries = new List<vm_officesummary>();

            foreach (var office in offices)
            {
                var summary = new vm_officesummary
                {
                    OfficeId = office.Id,
                    OfficeName = office.OfficeName,
                    VaultCount = office.Vaults.Count(v => v.IsActive),
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

                summaries.Add(summary);
            }

            // Cache the result
            _memoryCache.Set(cacheKey, summaries, CacheDuration);
            return summaries;
        }

        public async Task<decimal> CalculateProfitLossAsync(Guid officeId, DateTime startDate, DateTime endDate)
        {
            var transactions = await _context.Transactions
                .Include(t => t.Details)
                .Include(t => t.Vault)
                .Where(t => t.Vault.OfficeId == officeId &&
                           t.TransactionDate >= startDate &&
                           t.TransactionDate <= endDate &&
                           t.Status == TransactionStatus.Completed &&
                           t.Type == TransactionType.Exchange)
                .ToListAsync();

            // Sum up the profit from all exchange transactions
            decimal totalProfitLoss = transactions.Sum(t => t.Profit);

            return totalProfitLoss;
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
            // Clear cache when balance is updated
            ClearVaultCaches();

            // Get vault for later cache invalidation
            var vault = await _context.Vaults.FirstOrDefaultAsync(v => v.Id == data.vaultId);

            var balance = await _context.VaultBalances
                .Where(b => b.VaultId == data.vaultId && b.CurrencyId == data.currencyId).Include(x => x.Currency).FirstOrDefaultAsync();

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

            if (data.TransactionType != TransactionType.Exchange)
            {
                if (data.isEntireBalance)
                {
                    iType = TransactionType.Adjustment;
                    data.description = $"{balance.Currency.CurrencyName} elle düzeltildi {currentBalance:0.00} -> {balance.Balance:0.00} ({balance.Balance - currentBalance:0.00}) ";
                }
                else if (data.amount > 0)
                {
                    iType = TransactionType.Deposit;
                }
                else
                {
                    iType = TransactionType.Withdrawal;
                }
            }
            else
            {
                iType = TransactionType.Exchange;
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
                UserId = Guid.Parse(_validationService.GetUserID())
            };

            //if (!data.isEntireBalance) _context.VaultBalanceHistories.Add(nHistory);
            _context.VaultBalanceHistories.Add(nHistory);

            await _context.SaveChangesAsync();

            // Invalidate Z-report cache AFTER saving changes
            if (vault != null)
            {
                InvalidateZReportCache(vault.OfficeId, DateTime.Today);
                InvalidateZReportCache(vault.OfficeId, nHistory.CreatedDate.Date);
                InvalidateZReportCache(vault.OfficeId, DateTime.Now.Date);
            }
        }

        public async Task SaveVault(rm_savevault data)
        {
            // Clear cache when vault data is modified
            ClearVaultCaches();

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

            // Invalidate Z-report cache AFTER saving changes
            InvalidateZReportCache(data.OfficeId, DateTime.Today);
            InvalidateZReportCache(data.OfficeId, DateTime.Now.Date);
        }

        public async Task RemoveVault(Guid id)
        {
            // Clear cache when vault is removed
            ClearVaultCaches();

            var dbVault = await _context.Vaults.FindAsync(id);
            var officeId = dbVault?.OfficeId;

            var dbTransactions = _context.Transactions.Where(x => x.VaultId == dbVault.Id);
            _context.Transactions.RemoveRange(dbTransactions);
            await _context.SaveChangesAsync();
            _context.Vaults.Remove(dbVault);
            await _context.SaveChangesAsync();

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

            // Order by date descending (newest first)
            var histories = await query
                .OrderByDescending(vh => vh.CreatedDate)
                .ToListAsync();

            // Debug log to check deleted and ghost records
            var deletedCount = await _context.VaultBalanceHistories
                .CountAsync(vh => vh.Vault.OfficeId == officeId && vh.IsDeleted == true);
            var ghostCount = await _context.VaultBalanceHistories
                .CountAsync(vh => vh.Vault.OfficeId == officeId && vh.IsGhost == true);
            Console.WriteLine($"[VaultHistory Debug] Total: {histories.Count}, Deleted (excluded): {deletedCount}, Ghost (excluded): {ghostCount}");

            // Get user names
            var userIds = histories.Where(h => h.UserId.HasValue).Select(h => h.UserId.Value).Distinct().ToList();
            var users = await _context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => $"{u.Firstname} {u.Lastname}");

            // Double check no deleted records
            if (histories.Any(h => h.IsDeleted))
            {
                Console.WriteLine($"[CRITICAL ERROR] Found {histories.Count(h => h.IsDeleted)} deleted records that should have been excluded!");
            }

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
                    // Get current system balance
                    var systemBalance = await _context.VaultBalances
                        .Where(vb => vb.VaultId == data.VaultId && vb.CurrencyId == detail.CurrencyId)
                        .Select(vb => vb.Balance)
                        .FirstOrDefaultAsync();

                    var discrepancy = detail.ActualAmount - systemBalance;

                    if (Math.Abs(discrepancy) > 0.01m)
                    {
                        vaultCount.HasDiscrepancy = true;
                        var currency = await _context.Currencies
                            .Where(c => c.Id == detail.CurrencyId)
                            .Select(c => c.CurrencyCode)
                            .FirstOrDefaultAsync();
                        
                        discrepancyDetails.Add($"{currency}: Beklenen {systemBalance:F2}, Sayılan {detail.ActualAmount:F2}, Fark {discrepancy:F2}");
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
                _context.VaultCountDetails.AddRange(vaultCount.CountDetails);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ClearVaultCaches();
                return true;
            }
            catch
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

            var result = new List<vm_vaultcount>();

            foreach (var count in counts)
            {
                var user = await _context.Users
                    .Where(u => u.Id == count.UserId)
                    .Select(u => new { u.Username, Fullname = u.Firstname + " " + u.Lastname })
                    .FirstOrDefaultAsync();

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