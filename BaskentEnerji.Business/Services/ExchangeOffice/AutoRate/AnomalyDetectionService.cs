using System;
using System.Linq;
using System.Threading.Tasks;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;

namespace BaskentEnerji.Business.Services.ExchangeOffice.AutoRate
{
    /// <summary>
    /// Anormal kur değişimlerini tespit eden servis
    /// </summary>
    public class AnomalyDetectionService
    {
        /// <summary>
        /// Kur değişiminin anormal olup olmadığını kontrol et
        /// </summary>
        public AnomalyCheckResult CheckForAnomaly(
            decimal currentBuyRate,
            decimal currentSellRate,
            decimal newBuyRate,
            decimal newSellRate,
            decimal maxChangePercent)
        {
            var result = new AnomalyCheckResult
            {
                IsAnomaly = false,
                ChangePercent = 0,
                Reason = null
            };

            // Buy rate değişimi
            var buyChange = Math.Abs(newBuyRate - currentBuyRate) / currentBuyRate * 100;

            // Sell rate değişimi
            var sellChange = Math.Abs(newSellRate - currentSellRate) / currentSellRate * 100;

            // En yüksek değişimi al
            var maxChange = Math.Max(buyChange, sellChange);

            result.ChangePercent = maxChange;
            result.BuyRateChangePercent = buyChange;
            result.SellRateChangePercent = sellChange;

            // Anormal mi kontrol et
            if (maxChange > maxChangePercent)
            {
                result.IsAnomaly = true;
                result.Reason = $"Kur değişimi %{maxChange:F2} (eşik: %{maxChangePercent:F2}). ";

                if (buyChange > maxChangePercent)
                {
                    result.Reason += $"Alış kuru: {currentBuyRate:F4} → {newBuyRate:F4} (%{buyChange:F2}). ";
                }

                if (sellChange > maxChangePercent)
                {
                    result.Reason += $"Satış kuru: {currentSellRate:F4} → {newSellRate:F4} (%{sellChange:F2}).";
                }
            }

            return result;
        }

        /// <summary>
        /// Sıfır veya negatif rate kontrolü
        /// </summary>
        public AnomalyCheckResult CheckForInvalidRates(
            decimal newBuyRate,
            decimal newSellRate)
        {
            var result = new AnomalyCheckResult
            {
                IsAnomaly = false,
                ChangePercent = 0
            };

            if (newBuyRate <= 0 || newSellRate <= 0)
            {
                result.IsAnomaly = true;
                result.Reason = "Geçersiz kur değeri: Rate sıfır veya negatif olamaz.";
            }
            else if (newSellRate < newBuyRate)
            {
                result.IsAnomaly = true;
                result.Reason = "Geçersiz kur değeri: Satış kuru alış kurundan düşük olamaz.";
            }

            return result;
        }

        /// <summary>
        /// Spread kontrolü (alış-satış farkı)
        /// </summary>
        public AnomalyCheckResult CheckSpread(
            decimal buyRate,
            decimal sellRate,
            decimal minSpreadPercent = 0.1m,
            decimal maxSpreadPercent = 20m)
        {
            var result = new AnomalyCheckResult
            {
                IsAnomaly = false,
                ChangePercent = 0
            };

            if (buyRate <= 0 || sellRate <= 0)
            {
                return result;
            }

            var spread = ((sellRate - buyRate) / buyRate) * 100;

            if (spread < minSpreadPercent)
            {
                result.IsAnomaly = true;
                result.Reason = $"Spread çok düşük: %{spread:F2} (minimum: %{minSpreadPercent:F2})";
            }
            else if (spread > maxSpreadPercent)
            {
                result.IsAnomaly = true;
                result.Reason = $"Spread çok yüksek: %{spread:F2} (maksimum: %{maxSpreadPercent:F2})";
            }

            return result;
        }

        /// <summary>
        /// Tüm anomaly kontrollerini yap
        /// </summary>
        public AnomalyCheckResult PerformAllChecks(
            decimal? currentBuyRate,
            decimal? currentSellRate,
            decimal newBuyRate,
            decimal newSellRate,
            decimal maxChangePercent)
        {
            // 1. Geçersiz rate kontrolü
            var invalidCheck = CheckForInvalidRates(newBuyRate, newSellRate);
            if (invalidCheck.IsAnomaly)
            {
                return invalidCheck;
            }

            // 2. Spread kontrolü
            var spreadCheck = CheckSpread(newBuyRate, newSellRate);
            if (spreadCheck.IsAnomaly)
            {
                return spreadCheck;
            }

            // 3. Değişim kontrolü (sadece mevcut rate varsa)
            if (currentBuyRate.HasValue && currentSellRate.HasValue)
            {
                var changeCheck = CheckForAnomaly(
                    currentBuyRate.Value,
                    currentSellRate.Value,
                    newBuyRate,
                    newSellRate,
                    maxChangePercent);

                if (changeCheck.IsAnomaly)
                {
                    return changeCheck;
                }
            }

            // Anomaly yok
            return new AnomalyCheckResult
            {
                IsAnomaly = false,
                ChangePercent = 0,
                Reason = null
            };
        }
    }

    /// <summary>
    /// Anomaly kontrol sonucu
    /// </summary>
    public class AnomalyCheckResult
    {
        public bool IsAnomaly { get; set; }
        public decimal ChangePercent { get; set; }
        public decimal BuyRateChangePercent { get; set; }
        public decimal SellRateChangePercent { get; set; }
        public string? Reason { get; set; }
    }
}
