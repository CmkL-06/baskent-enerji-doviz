using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmileMedical.Entity.Entities.ExchangeOffice.Currency;
using SmileMedical.Data.Contexts;

namespace SmileMedical.Business.Services.ExchangeOffice.AutoRate
{
    /// <summary>
    /// Otomatik kur güncelleme ana servisi
    /// </summary>
    public class AutoRateUpdateService
    {
        private readonly SmileMedicalDbContext _context;
        private readonly RateCalculationService _calculationService;
        private readonly AnomalyDetectionService _anomalyService;
        private readonly List<IExternalRateProvider> _providers;

        public AutoRateUpdateService(
            SmileMedicalDbContext context,
            RateCalculationService calculationService,
            AnomalyDetectionService anomalyService,
            IEnumerable<IExternalRateProvider> providers)
        {
            _context = context;
            _calculationService = calculationService;
            _anomalyService = anomalyService;
            _providers = providers.ToList();
        }

        /// <summary>
        /// Test - Only fetch rates without updating
        /// </summary>
        public async Task<AutoUpdateResultDto> TestFetchOnlyAsync(ExchangeSettings settings)
        {
            var result = new AutoUpdateResultDto();
            try
            {
                var externalRates = await FetchFromAllSourcesAsync(settings);
                result.ExternalRates = externalRates;
                result.Success = true;
                result.Summary = $"TEST MODE: Fetched {externalRates.Count} rates from external sources (NOT UPDATED)";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Error fetching rates: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Otomatik güncelleme yap
        /// </summary>
        public async Task<AutoUpdateResultDto> UpdateRatesAsync(AutoUpdateRequestDto request)
        {
            var result = new AutoUpdateResultDto
            {
                Success = false,
                TotalRatesProcessed = 0,
                RatesUpdated = 0,
                RatesPendingApproval = 0,
                RatesSkipped = 0
            };

            try
            {
                // 1. Ayarları yükle
                var settings = await _context.Set<ExchangeSettings>()
                    .OrderBy(s => s.CreatedDate)
                    .FirstOrDefaultAsync();
                if (settings == null)
                {
                    result.Errors.Add("Exchange settings not found. Please configure settings first.");
                    return result;
                }

                // 2. Dış kaynaklardan veri çek
                var externalRates = await FetchFromAllSourcesAsync(settings);
                if (!externalRates.Any(r => r.IsValid))
                {
                    result.Errors.Add("No valid rates fetched from external sources.");
                    return result;
                }

                // 3. Cache'e kaydet
                await SaveToCache(externalRates);

                // 4. İşlenecek ofisleri belirle
                var officeIds = await GetOfficeIdsToProcess(request.OfficeId);
                if (!officeIds.Any())
                {
                    result.Errors.Add("No offices to process.");
                    return result;
                }

                // 5. Her ofis için kurları güncelle
                foreach (var officeId in officeIds)
                {
                    var officeResult = await UpdateOfficeRatesAsync(
                        officeId,
                        externalRates,
                        settings,
                        request.TriggeredBy ?? "SYSTEM");

                    result.TotalRatesProcessed += officeResult.TotalProcessed;
                    result.RatesUpdated += officeResult.Updated;
                    result.RatesPendingApproval += officeResult.PendingApproval;
                    result.RatesSkipped += officeResult.Skipped;
                    result.Errors.AddRange(officeResult.Errors);
                    result.Warnings.AddRange(officeResult.Warnings);
                }

                // 6. Son güncelleme zamanını kaydet
                settings.LastAutoUpdate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                result.Success = true;
                result.Summary = $"Processed {result.TotalRatesProcessed} rates: {result.RatesUpdated} updated, " +
                               $"{result.RatesPendingApproval} pending approval, {result.RatesSkipped} skipped.";

                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Fatal error: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Tüm kaynaklardan veri çek
        /// </summary>
        private async Task<List<ExternalRateDto>> FetchFromAllSourcesAsync(ExchangeSettings settings)
        {
            var allRates = new List<ExternalRateDto>();

            // Enabled provider'ları filtrele
            var activeProviders = _providers.Where(p =>
            {
                if (p.SourceKey == "TCMB") return settings.UseTcmb;
                if (p.SourceKey.StartsWith("DOVIZ_COM")) return settings.UseDovizCom;
                if (p.SourceKey == "BINANCE") return settings.UseBinance;
                return false;
            }).ToList();

            // Paralel olarak tüm provider'lardan çek
            var tasks = activeProviders.Select(p => p.FetchRatesAsync()).ToList();
            var results = await Task.WhenAll(tasks);

            foreach (var rates in results)
            {
                allRates.AddRange(rates);
            }

            return allRates;
        }

        /// <summary>
        /// External rate'leri cache'e kaydet
        /// </summary>
        private async Task SaveToCache(List<ExternalRateDto> rates)
        {
            var cacheEntries = rates.Select(r => new ExternalRateCache
            {
                Source = r.Source,
                CurrencyCode = r.CurrencyCode,
                TargetCurrencyCode = r.TargetCurrencyCode,
                BuyRate = r.BuyRate,
                SellRate = r.SellRate,
                SpreadPercent = r.SpreadPercent,
                FetchedAt = r.FetchedAt,
                IsValid = r.IsValid,
                ErrorMessage = r.ErrorMessage,
                SourceUrl = r.SourceUrl
            }).ToList();

            await _context.Set<ExternalRateCache>().AddRangeAsync(cacheEntries);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// İşlenecek ofis ID'lerini getir
        /// </summary>
        private async Task<List<Guid>> GetOfficeIdsToProcess(Guid? specificOfficeId)
        {
            if (specificOfficeId.HasValue)
            {
                return new List<Guid> { specificOfficeId.Value };
            }

            // Tüm ofisleri getir
            var offices = await _context.Set<SmileMedical.Entity.Entities.ExchangeOffice.Office.Office>()
                .Where(o => o.IsActive)
                .Select(o => o.Id)
                .ToListAsync();

            return offices;
        }

        /// <summary>
        /// Bir ofis için tüm kurları güncelle
        /// </summary>
        private async Task<OfficeUpdateResult> UpdateOfficeRatesAsync(
            Guid officeId,
            List<ExternalRateDto> externalRates,
            ExchangeSettings settings,
            string triggeredBy)
        {
            var result = new OfficeUpdateResult();

            // Ofis için aktif kurları getir
            var activeRates = await _context.Set<ExchangeRate>()
                .Where(r => r.OfficeId == officeId && r.IsActive)
                .Include(r => r.SourceCurrency)
                .Include(r => r.TargetCurrency)
                .ToListAsync();

            foreach (var currentRate in activeRates)
            {
                result.TotalProcessed++;

                try
                {
                    // Rate hesapla
                    var calculation = _calculationService.CalculateRate(
                        currentRate.SourceCurrency.CurrencyCode,
                        currentRate.TargetCurrency.CurrencyCode,
                        externalRates,
                        settings.RateSelectionStrategy ?? "BEST_BUY",
                        GetProfitMarginForPair(currentRate, settings),
                        currentRate.BuyRate,
                        currentRate.SellRate);

                    if (calculation.CalculatedBuyRate == 0 || calculation.CalculatedSellRate == 0)
                    {
                        result.Skipped++;
                        result.Warnings.Add($"Skipped {currentRate.SourceCurrency.CurrencyCode}/{currentRate.TargetCurrency.CurrencyCode}: No valid calculation");
                        continue;
                    }

                    // Anomaly kontrol et
                    var anomalyCheck = _anomalyService.PerformAllChecks(
                        currentRate.BuyRate,
                        currentRate.SellRate,
                        calculation.CalculatedBuyRate,
                        calculation.CalculatedSellRate,
                        settings.MaxPriceChangePercent);

                    if (anomalyCheck.IsAnomaly && settings.RequireApprovalAboveThreshold)
                    {
                        // Pending approval'a ekle
                        await CreatePendingApprovalAsync(
                            officeId,
                            currentRate,
                            calculation,
                            anomalyCheck);

                        result.PendingApproval++;
                    }
                    else
                    {
                        // Direkt güncelle
                        await UpdateRateDirectlyAsync(
                            currentRate,
                            calculation.CalculatedBuyRate,
                            calculation.CalculatedSellRate,
                            calculation.SourceRates.Select(r => r.Source).ToList(),
                            triggeredBy);

                        result.Updated++;
                    }
                }
                catch (Exception ex)
                {
                    result.Skipped++;
                    result.Errors.Add($"Error processing {currentRate.SourceCurrency.CurrencyCode}/{currentRate.TargetCurrency.CurrencyCode}: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Kar marjını belirle (önce dövize özel, sonra TRY bazlı, cross-fiat, crypto)
        /// </summary>
        private decimal GetProfitMarginForPair(ExchangeRate rate, ExchangeSettings settings)
        {
            var targetCode = rate.TargetCurrency.CurrencyCode;
            var sourceCode = rate.SourceCurrency.CurrencyCode;

            // Önce dövize özel marjları kontrol et
            if (!string.IsNullOrEmpty(settings.CurrencySpecificMargins))
            {
                try
                {
                    var margins = JsonSerializer.Deserialize<Dictionary<string, decimal>>(settings.CurrencySpecificMargins);
                    if (margins != null && margins.ContainsKey(sourceCode))
                    {
                        return margins[sourceCode];
                    }
                }
                catch
                {
                    // JSON parse hatası durumunda genel marjlara devam et
                }
            }

            // Crypto mu?
            var cryptoCodes = new[] { "USDT", "BTC", "ETH", "USDC" };
            if (cryptoCodes.Contains(sourceCode) || cryptoCodes.Contains(targetCode))
            {
                return settings.CryptoMarginPercent;
            }

            // TRY bazlı mı?
            if (targetCode == "TRY" || sourceCode == "TRY")
            {
                return settings.TryBasedMarginPercent;
            }

            // Cross-fiat
            return settings.CrossFiatMarginPercent;
        }

        /// <summary>
        /// Pending approval kaydı oluştur
        /// </summary>
        private async Task CreatePendingApprovalAsync(
            Guid officeId,
            ExchangeRate currentRate,
            RateCalculationResultDto calculation,
            AnomalyCheckResult anomaly)
        {
            var pending = new PendingRateApproval
            {
                OfficeId = officeId,
                SourceCurrencyId = currentRate.SourceCurrencyId,
                TargetCurrencyId = currentRate.TargetCurrencyId,
                CurrentBuyRate = currentRate.BuyRate,
                CurrentSellRate = currentRate.SellRate,
                ProposedBuyRate = calculation.CalculatedBuyRate,
                ProposedSellRate = calculation.CalculatedSellRate,
                ChangePercent = anomaly.ChangePercent,
                Reason = anomaly.Reason,
                SourceData = JsonSerializer.Serialize(calculation.SourceRates),
                Status = "PENDING"
            };

            await _context.Set<PendingRateApproval>().AddAsync(pending);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Rate'i direkt güncelle
        /// </summary>
        private async Task UpdateRateDirectlyAsync(
            ExchangeRate currentRate,
            decimal newBuyRate,
            decimal newSellRate,
            List<string?> dataSources,
            string triggeredBy)
        {
            // History kaydı oluştur
            var history = new ExchangeRateHistory
            {
                OfficeId = currentRate.OfficeId,
                SourceCurrencyId = currentRate.SourceCurrencyId,
                TargetCurrencyId = currentRate.TargetCurrencyId,
                OldBuyRate = currentRate.BuyRate,
                OldSellRate = currentRate.SellRate,
                NewBuyRate = newBuyRate,
                NewSellRate = newSellRate,
                ChangePercent = Math.Max(
                    Math.Abs(newBuyRate - currentRate.BuyRate) / currentRate.BuyRate * 100,
                    Math.Abs(newSellRate - currentRate.SellRate) / currentRate.SellRate * 100),
                UpdateSource = "AUTO_SYSTEM",
                DataSources = JsonSerializer.Serialize(dataSources.Where(s => !string.IsNullOrEmpty(s)).Distinct()),
                IsApproved = true
            };

            await _context.Set<ExchangeRateHistory>().AddAsync(history);

            // Rate'i güncelle
            currentRate.BuyRate = newBuyRate;
            currentRate.SellRate = newSellRate;
            currentRate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private class OfficeUpdateResult
        {
            public int TotalProcessed { get; set; }
            public int Updated { get; set; }
            public int PendingApproval { get; set; }
            public int Skipped { get; set; }
            public List<string> Errors { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
        }
    }
}
