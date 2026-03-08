using System;
using System.Collections.Generic;
using System.Linq;

namespace BaskentEnerji.Business.Services.ExchangeOffice.AutoRate
{
    /// <summary>
    /// Kur hesaplama servisi - 3 strateji destekler
    /// </summary>
    public class RateCalculationService
    {
        /// <summary>
        /// Stratejiye göre rate hesapla
        /// </summary>
        public RateCalculationResultDto CalculateRate(
            string sourceCurrency,
            string targetCurrency,
            List<ExternalRateDto> sourceRates,
            string strategy,
            decimal profitMarginPercent,
            decimal? currentBuyRate = null,
            decimal? currentSellRate = null)
        {
            var result = new RateCalculationResultDto
            {
                CurrencyPair = $"{sourceCurrency}/{targetCurrency}",
                StrategyUsed = strategy,
                ProfitMarginPercent = profitMarginPercent,
                SourceRates = sourceRates.Where(r => r.IsValid).ToList()
            };

            if (!sourceRates.Any(r => r.IsValid))
            {
                result.Calculation = "No valid rates available";
                return result;
            }

            // Doğrudan rate mi yoksa cross-rate mi?
            var directRates = sourceRates
                .Where(r => r.IsValid &&
                           r.CurrencyCode == sourceCurrency &&
                           r.TargetCurrencyCode == targetCurrency)
                .ToList();

            if (directRates.Any())
            {
                // Direkt rate var (örn: USD/TRY)
                CalculateDirectRate(result, directRates, strategy, profitMarginPercent);
            }
            else
            {
                // Cross-rate hesapla (örn: EUR/USD)
                CalculateCrossRate(result, sourceCurrency, targetCurrency, sourceRates, strategy, profitMarginPercent);
            }

            // Değişim kontrolü
            if (currentBuyRate.HasValue && currentSellRate.HasValue)
            {
                var buyChange = Math.Abs(result.CalculatedBuyRate - currentBuyRate.Value) / currentBuyRate.Value * 100;
                var sellChange = Math.Abs(result.CalculatedSellRate - currentSellRate.Value) / currentSellRate.Value * 100;
                var maxChange = Math.Max(buyChange, sellChange);

                result.Calculation += $"\n\nPrevious: Buy {currentBuyRate:F4}, Sell {currentSellRate:F4}";
                result.Calculation += $"\nChange: {maxChange:F2}%";
            }

            return result;
        }

        private void CalculateDirectRate(
            RateCalculationResultDto result,
            List<ExternalRateDto> rates,
            string strategy,
            decimal profitMarginPercent)
        {
            var buyRates = rates.Select(r => r.BuyRate).ToList();
            var sellRates = rates.Select(r => r.SellRate).ToList();

            decimal baseBuyRate, baseSellRate;
            string calculation;

            switch (strategy)
            {
                case "BEST_BUY":
                    // Müşteriden en yüksek fiyattan al, müşteriye en düşük fiyattan sat + kar
                    baseBuyRate = buyRates.Max();
                    baseSellRate = sellRates.Min();
                    calculation = $"Strategy: BEST_BUY\n" +
                                $"Buy: MAX({string.Join(", ", buyRates.Select(r => r.ToString("F4")))}) = {baseBuyRate:F4}\n" +
                                $"Sell: MIN({string.Join(", ", sellRates.Select(r => r.ToString("F4")))}) = {baseSellRate:F4}";
                    break;

                case "AVERAGE":
                    // Ortalama fiyat
                    baseBuyRate = buyRates.Average();
                    baseSellRate = sellRates.Average();
                    calculation = $"Strategy: AVERAGE\n" +
                                $"Buy: AVG({string.Join(", ", buyRates.Select(r => r.ToString("F4")))}) = {baseBuyRate:F4}\n" +
                                $"Sell: AVG({string.Join(", ", sellRates.Select(r => r.ToString("F4")))}) = {baseSellRate:F4}";
                    break;

                case "COMPETITIVE":
                    // Piyasadan biraz daha iyi fiyat
                    baseBuyRate = buyRates.Max() * 0.98m; // %2 daha düşük al
                    baseSellRate = sellRates.Min() * 1.02m; // %2 daha yüksek sat
                    calculation = $"Strategy: COMPETITIVE\n" +
                                $"Buy: MAX({string.Join(", ", buyRates.Select(r => r.ToString("F4")))}) * 0.98 = {baseBuyRate:F4}\n" +
                                $"Sell: MIN({string.Join(", ", sellRates.Select(r => r.ToString("F4")))}) * 1.02 = {baseSellRate:F4}";
                    break;

                default:
                    baseBuyRate = buyRates.Average();
                    baseSellRate = sellRates.Average();
                    calculation = "Strategy: DEFAULT (Average)";
                    break;
            }

            // Kar marjı HEM alış HEM satış kuruna uygulanır
            result.CalculatedBuyRate = baseBuyRate * (1 - profitMarginPercent / 100); // Daha ucuza al
            result.CalculatedSellRate = baseSellRate * (1 + profitMarginPercent / 100); // Daha pahalıya sat

            calculation += $"\n\nProfit Margin: {profitMarginPercent}%\n" +
                          $"Final Buy: {baseBuyRate:F4} * {(1 - profitMarginPercent / 100):F4} = {result.CalculatedBuyRate:F4}\n" +
                          $"Final Sell: {baseSellRate:F4} * {(1 + profitMarginPercent / 100):F4} = {result.CalculatedSellRate:F4}";

            calculation += $"\n\nSources: {string.Join(", ", rates.Select(r => r.Source))}";

            result.Calculation = calculation;
        }

        private void CalculateCrossRate(
            RateCalculationResultDto result,
            string sourceCurrency,
            string targetCurrency,
            List<ExternalRateDto> allRates,
            string strategy,
            decimal profitMarginPercent)
        {
            // Cross-rate: EUR/USD gibi
            // EUR/TRY ve USD/TRY kurlarından hesaplanacak
            // EUR/USD = EUR/TRY ÷ USD/TRY

            var baseCurrency = "TRY"; // Ana para birimi

            var sourceToBase = allRates
                .Where(r => r.IsValid && r.CurrencyCode == sourceCurrency && r.TargetCurrencyCode == baseCurrency)
                .ToList();

            var targetToBase = allRates
                .Where(r => r.IsValid && r.CurrencyCode == targetCurrency && r.TargetCurrencyCode == baseCurrency)
                .ToList();

            if (!sourceToBase.Any() || !targetToBase.Any())
            {
                result.Calculation = $"Cannot calculate cross-rate: Missing {sourceCurrency}/TRY or {targetCurrency}/TRY rates";
                return;
            }

            // Stratejiye göre base rate'leri hesapla
            decimal sourceToBaseBuy, sourceToBaseSell;
            decimal targetToBaseBuy, targetToBaseSell;

            switch (strategy)
            {
                case "BEST_BUY":
                    sourceToBaseBuy = sourceToBase.Max(r => r.BuyRate);
                    sourceToBaseSell = sourceToBase.Min(r => r.SellRate);
                    targetToBaseBuy = targetToBase.Max(r => r.BuyRate);
                    targetToBaseSell = targetToBase.Min(r => r.SellRate);
                    break;

                case "AVERAGE":
                    sourceToBaseBuy = sourceToBase.Average(r => r.BuyRate);
                    sourceToBaseSell = sourceToBase.Average(r => r.SellRate);
                    targetToBaseBuy = targetToBase.Average(r => r.BuyRate);
                    targetToBaseSell = targetToBase.Average(r => r.SellRate);
                    break;

                case "COMPETITIVE":
                    sourceToBaseBuy = sourceToBase.Max(r => r.BuyRate) * 0.98m;
                    sourceToBaseSell = sourceToBase.Min(r => r.SellRate) * 1.02m;
                    targetToBaseBuy = targetToBase.Max(r => r.BuyRate) * 0.98m;
                    targetToBaseSell = targetToBase.Min(r => r.SellRate) * 1.02m;
                    break;

                default:
                    sourceToBaseBuy = sourceToBase.Average(r => r.BuyRate);
                    sourceToBaseSell = sourceToBase.Average(r => r.SellRate);
                    targetToBaseBuy = targetToBase.Average(r => r.BuyRate);
                    targetToBaseSell = targetToBase.Average(r => r.SellRate);
                    break;
            }

            // Cross-rate hesaplama
            // EUR/USD = EUR/TRY ÷ USD/TRY
            var crossBuyRate = sourceToBaseBuy / targetToBaseSell;
            var crossSellRate = sourceToBaseSell / targetToBaseBuy;

            // Kar marjı ekle
            result.CalculatedBuyRate = crossBuyRate;
            result.CalculatedSellRate = crossSellRate * (1 + profitMarginPercent / 100);

            result.Calculation = $"Strategy: {strategy} (Cross-Rate)\n\n" +
                               $"{sourceCurrency}/TRY: Buy={sourceToBaseBuy:F4}, Sell={sourceToBaseSell:F4}\n" +
                               $"{targetCurrency}/TRY: Buy={targetToBaseBuy:F4}, Sell={targetToBaseSell:F4}\n\n" +
                               $"Cross-Rate Formula:\n" +
                               $"{sourceCurrency}/{targetCurrency} Buy = {sourceToBaseBuy:F4} ÷ {targetToBaseSell:F4} = {crossBuyRate:F6}\n" +
                               $"{sourceCurrency}/{targetCurrency} Sell = {sourceToBaseSell:F4} ÷ {targetToBaseBuy:F4} = {crossSellRate:F6}\n\n" +
                               $"Profit Margin: {profitMarginPercent}%\n" +
                               $"Final Buy: {result.CalculatedBuyRate:F6}\n" +
                               $"Final Sell: {crossSellRate:F6} * {(1 + profitMarginPercent / 100):F4} = {result.CalculatedSellRate:F6}";
        }
    }
}
