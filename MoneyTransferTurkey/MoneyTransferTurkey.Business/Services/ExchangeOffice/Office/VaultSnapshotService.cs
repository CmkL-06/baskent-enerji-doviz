using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MoneyTransferTurkey.Business.Infrastructure.ExchangeOffice.Office;
using MoneyTransferTurkey.Data.Contexts;
using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Office;
using MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Office;
using MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Services.ExchangeOffice.Office
{
    /// <summary>
    /// Service for managing vault balance snapshots
    /// Provides functionality to capture and query historical vault balances
    /// </summary>
    public class VaultSnapshotService : IVaultSnapshotService
    {
        private readonly MoneyTransferTurkeyDbContext _context;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _memoryCache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

        public VaultSnapshotService(MoneyTransferTurkeyDbContext context, IMapper mapper, IMemoryCache memoryCache)
        {
            _context = context;
            _mapper = mapper;
            _memoryCache = memoryCache;
        }

        public async Task<vm_vaultbalancesnapshot> CreateSnapshotAsync(rm_createsnapshot data, Guid userId)
        {
            // Validate office exists
            var officeExists = await _context.Offices.AnyAsync(o => o.Id == data.OfficeId);
            if (!officeExists)
            {
                throw new Exception("Office not found");
            }

            // Get all active vaults for the office
            var vaults = await _context.Vaults
                .Where(v => v.OfficeId == data.OfficeId && v.IsActive)
                .ToListAsync();

            if (!vaults.Any())
            {
                throw new Exception("No active vaults found for this office");
            }

            // Get all vault balances for these vaults
            var vaultIds = vaults.Select(v => v.Id).ToList();
            var vaultBalances = await _context.VaultBalances
                .Include(vb => vb.Currency)
                .Include(vb => vb.Vault)
                .Where(vb => vaultIds.Contains(vb.VaultId))
                .ToListAsync();

            if (!vaultBalances.Any())
            {
                throw new Exception("No vault balances found to snapshot");
            }

            // Create snapshot
            var snapshot = new VaultBalanceSnapshot
            {
                Id = Guid.NewGuid(),
                OfficeId = data.OfficeId,
                UserId = userId,
                SnapshotDate = DateTime.Now,
                Description = data.Description,
                CreatedDate = DateTime.Now
            };

            _context.VaultBalanceSnapshots.Add(snapshot);

            // Create snapshot details for each vault balance
            var snapshotDetails = vaultBalances.Select(vb => new VaultBalanceSnapshotDetail
            {
                Id = Guid.NewGuid(),
                SnapshotId = snapshot.Id,
                VaultId = vb.VaultId,
                CurrencyId = vb.CurrencyId,
                Balance = vb.Balance,
                ReservedAmount = vb.ReservedAmount,
                CreatedDate = DateTime.Now
            }).ToList();

            _context.VaultBalanceSnapshotDetails.AddRange(snapshotDetails);

            await _context.SaveChangesAsync();

            // Return the created snapshot with details
            return await GetSnapshotByIdAsync(snapshot.Id);
        }

        public async Task<List<vm_vaultbalancesnapshot>> GetSnapshotsByOfficeAsync(Guid officeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.VaultBalanceSnapshots
                .Include(s => s.Office)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Currency)
                .Where(s => s.OfficeId == officeId);

            if (startDate.HasValue)
            {
                query = query.Where(s => s.SnapshotDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.SnapshotDate <= endDate.Value);
            }

            var snapshots = await query
                .OrderByDescending(s => s.SnapshotDate)
                .ToListAsync();

            // Get user names
            var userIds = snapshots.Select(s => s.UserId).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.Firstname + " " + u.Lastname })
                .ToDictionaryAsync(u => u.Id, u => u.Name);

            // Get base currency and exchange rates
            var baseCurrencyId = await GetBaseCurrencyIdAsync();
            var allCurrencyIds = snapshots.SelectMany(s => s.Details.Select(d => d.CurrencyId)).Distinct().ToList();
            var exchangeRates = await GetExchangeRatesForCurrenciesAsync(allCurrencyIds);

            var result = new List<vm_vaultbalancesnapshot>();

            foreach (var s in snapshots)
            {
                // Calculate total value in base currency for this snapshot
                decimal totalValue = 0;
                foreach (var detail in s.Details)
                {
                    decimal valueInBase;
                    if (detail.CurrencyId == baseCurrencyId)
                    {
                        valueInBase = detail.Balance;
                    }
                    else
                    {
                        decimal rate = 0;
                        if (exchangeRates.TryGetValue(detail.CurrencyId, out var foundRate))
                            rate = foundRate;
                        valueInBase = detail.Balance * rate;
                    }
                    totalValue += valueInBase;
                }

                result.Add(new vm_vaultbalancesnapshot
                {
                    Id = s.Id,
                    OfficeId = s.OfficeId,
                    OfficeName = s.Office.OfficeName,
                    UserId = s.UserId,
                    UserName = users.ContainsKey(s.UserId) ? users[s.UserId] : "Unknown",
                    SnapshotDate = s.SnapshotDate,
                    Description = s.Description,
                    CreatedDate = s.CreatedDate,
                    TotalVaults = s.Details.Select(d => d.VaultId).Distinct().Count(),
                    TotalCurrencies = s.Details.Where(d => d.Balance > 0).Select(d => d.CurrencyId).Distinct().Count(), // Only count non-zero balances
                    TotalValueInBaseCurrency = totalValue,
                    Details = new List<vm_vaultbalancesnapshotdetail>() // Empty for list view
                });
            }

            return result;
        }

        public async Task<vm_vaultbalancesnapshot> GetSnapshotByIdAsync(Guid snapshotId)
        {
            var snapshot = await _context.VaultBalanceSnapshots
                .Include(s => s.Office)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Vault)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Currency)
                .FirstOrDefaultAsync(s => s.Id == snapshotId);

            if (snapshot == null)
            {
                throw new Exception("Snapshot not found");
            }

            // Get user name
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == snapshot.UserId);
            var userName = user != null ? $"{user.Firstname} {user.Lastname}" : "Unknown";

            // Get base currency and exchange rates
            var baseCurrencyId = await GetBaseCurrencyIdAsync();
            var currencyIds = snapshot.Details.Select(d => d.CurrencyId).Distinct().ToList();
            var exchangeRates = await GetExchangeRatesForCurrenciesAsync(currencyIds);

            decimal totalValue = 0;
            var details = new List<vm_vaultbalancesnapshotdetail>();

            foreach (var d in snapshot.Details)
            {
                decimal valueInBase;
                decimal exchangeRate;

                if (d.CurrencyId == baseCurrencyId)
                {
                    valueInBase = d.Balance;
                    exchangeRate = 1;
                }
                else
                {
                    if (exchangeRates.TryGetValue(d.CurrencyId, out var foundRate))
                        exchangeRate = foundRate;
                    else
                        exchangeRate = 0;
                    valueInBase = d.Balance * exchangeRate;
                }

                totalValue += valueInBase;

                details.Add(new vm_vaultbalancesnapshotdetail
                {
                    Id = d.Id,
                    SnapshotId = d.SnapshotId,
                    VaultId = d.VaultId,
                    VaultName = d.Vault.Name,
                    CurrencyId = d.CurrencyId,
                    CurrencyCode = d.Currency.CurrencyCode,
                    CurrencyName = d.Currency.CurrencyName,
                    Balance = d.Balance,
                    ReservedAmount = d.ReservedAmount,
                    ValueInBaseCurrency = valueInBase,
                    ExchangeRateToBase = exchangeRate,
                    CreatedDate = d.CreatedDate
                });
            }

            var result = new vm_vaultbalancesnapshot
            {
                Id = snapshot.Id,
                OfficeId = snapshot.OfficeId,
                OfficeName = snapshot.Office.OfficeName,
                UserId = snapshot.UserId,
                UserName = userName,
                SnapshotDate = snapshot.SnapshotDate,
                Description = snapshot.Description,
                CreatedDate = snapshot.CreatedDate,
                TotalVaults = snapshot.Details.Select(d => d.VaultId).Distinct().Count(),
                TotalCurrencies = snapshot.Details.Where(d => d.Balance > 0).Select(d => d.CurrencyId).Distinct().Count(), // Only count non-zero balances
                TotalValueInBaseCurrency = totalValue,
                Details = details.OrderByDescending(d => d.ValueInBaseCurrency).ToList() // Order by value descending
            };

            return result;
        }

        public async Task<List<vm_vaultbalancesnapshot>> GetSnapshotsByDateAsync(Guid officeId, DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            return await GetSnapshotsByOfficeAsync(officeId, startOfDay, endOfDay);
        }

        public async Task<bool> DeleteSnapshotAsync(Guid snapshotId)
        {
            var snapshot = await _context.VaultBalanceSnapshots
                .Include(s => s.Details)
                .FirstOrDefaultAsync(s => s.Id == snapshotId);

            if (snapshot == null)
            {
                return false;
            }

            // EF Core will cascade delete the details due to cascade configuration
            _context.VaultBalanceSnapshots.Remove(snapshot);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<object> CompareSnapshotsAsync(Guid snapshotId1, Guid snapshotId2)
        {
            var snapshot1 = await _context.VaultBalanceSnapshots
                .Include(s => s.Details)
                    .ThenInclude(d => d.Vault)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Currency)
                .FirstOrDefaultAsync(s => s.Id == snapshotId1);

            var snapshot2 = await _context.VaultBalanceSnapshots
                .Include(s => s.Details)
                    .ThenInclude(d => d.Vault)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Currency)
                .FirstOrDefaultAsync(s => s.Id == snapshotId2);

            if (snapshot1 == null || snapshot2 == null)
            {
                throw new Exception("One or both snapshots not found");
            }

            if (snapshot1.OfficeId != snapshot2.OfficeId)
            {
                throw new Exception("Cannot compare snapshots from different offices");
            }

            // Group details by vault and currency
            var details1 = snapshot1.Details
                .GroupBy(d => new { d.VaultId, d.CurrencyId })
                .ToDictionary(g => g.Key, g => g.First());

            var details2 = snapshot2.Details
                .GroupBy(d => new { d.VaultId, d.CurrencyId })
                .ToDictionary(g => g.Key, g => g.First());

            // Find all unique vault-currency combinations
            var allKeys = details1.Keys.Union(details2.Keys).ToList();

            var comparisons = allKeys.Select(key =>
            {
                var detail1 = details1.ContainsKey(key) ? details1[key] : null;
                var detail2 = details2.ContainsKey(key) ? details2[key] : null;

                var balance1 = detail1?.Balance ?? 0;
                var balance2 = detail2?.Balance ?? 0;
                var difference = balance2 - balance1;

                return new
                {
                    VaultId = key.VaultId,
                    VaultName = detail1?.Vault.Name ?? detail2?.Vault.Name,
                    CurrencyId = key.CurrencyId,
                    CurrencyCode = detail1?.Currency.CurrencyCode ?? detail2?.Currency.CurrencyCode,
                    CurrencyName = detail1?.Currency.CurrencyName ?? detail2?.Currency.CurrencyName,
                    Snapshot1Balance = balance1,
                    Snapshot2Balance = balance2,
                    Difference = difference,
                    PercentageChange = balance1 != 0 ? (difference / balance1) * 100 : 0
                };
            }).OrderBy(c => c.VaultName).ThenBy(c => c.CurrencyCode).ToList();

            return new
            {
                Snapshot1 = new
                {
                    snapshot1.Id,
                    snapshot1.SnapshotDate,
                    snapshot1.Description
                },
                Snapshot2 = new
                {
                    snapshot2.Id,
                    snapshot2.SnapshotDate,
                    snapshot2.Description
                },
                Comparisons = comparisons,
                Summary = new
                {
                    TotalVaults = comparisons.Select(c => c.VaultId).Distinct().Count(),
                    TotalCurrencies = comparisons.Select(c => c.CurrencyId).Distinct().Count(),
                    TotalDifferences = comparisons.Count(c => c.Difference != 0)
                }
            };
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
    }
}
