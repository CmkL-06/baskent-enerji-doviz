using System;
using System.Collections.Generic;
using System.Linq;

namespace BaskentEnerji.Business.Services.User
{
    public class BulkEntryHeuristicResult
    {
        public int TransactionCount { get; set; }
        public DateTime? FirstTransactionAt { get; set; }
        public DateTime? LastTransactionAt { get; set; }
        public bool IsBulkEntrySuspected { get; set; }
        public int BackdatedCount { get; set; }
        public double? TodayRatio { get; set; }
        public double? BaselineRatio { get; set; }
    }

    /// <summary>
    /// Bir günlük işlem listesindeki "toplu/şüpheli giriş" (çok sayıda işlemin
    /// çok dar bir zaman diliminde kaydedilmesi) ve "geriye tarihli giriş"
    /// (kayıt anı ile iş tarihi arasında büyük fark) desenini tespit eder.
    /// Hem GetUserDailyDetail hem AlertService.CheckStaffDailyAnomaliesAsync
    /// aynı mantığı kullanır — burada tek yerde tutulur.
    ///
    /// "Toplu giriş" kararı kişinin KENDİ geçmiş ortalamasına göre verilir
    /// (sabit bir eşik yerine): bugünün dağılım oranı, kişinin son günlerdeki
    /// ortalama oranının belirgin altındaysa (BaselineDeviationFactor) şüpheli
    /// sayılır. Yeterli geçmiş yoksa (yeni personel), sabit bir varsayılan
    /// baseline (DefaultBaselineRatio) kullanılır.
    /// </summary>
    public static class BulkEntryHeuristic
    {
        private const int BucketMinutes = 10;
        private const int BackdateThresholdHours = 2;
        private const int MinTransactionsForCheck = 5;
        private const int MinHistoricalDaysForPersonalBaseline = 3;
        private const double DefaultBaselineRatio = 0.5;
        private const double BaselineDeviationFactor = 0.5;

        /// <summary>Bir günün "dağılım oranı" — kullanılan farklı zaman dilimi sayısı / işlem sayısı.</summary>
        public static double? ComputeSpreadRatio(IReadOnlyCollection<DateTime> createdDates)
        {
            if (createdDates.Count == 0)
                return null;

            var distinctBuckets = createdDates
                .Select(d => d.Ticks / TimeSpan.FromMinutes(BucketMinutes).Ticks)
                .Distinct()
                .Count();

            return (double)distinctBuckets / createdDates.Count;
        }

        public static BulkEntryHeuristicResult Evaluate(
            IReadOnlyCollection<(DateTime CreatedDate, DateTime TransactionDate)> transactions,
            IReadOnlyCollection<double> historicalDailyRatios)
        {
            var result = new BulkEntryHeuristicResult { TransactionCount = transactions.Count };
            if (transactions.Count == 0)
                return result;

            result.FirstTransactionAt = transactions.Min(t => t.CreatedDate);
            result.LastTransactionAt = transactions.Max(t => t.CreatedDate);
            result.BackdatedCount = transactions.Count(t => (t.CreatedDate - t.TransactionDate).TotalHours > BackdateThresholdHours);

            if (transactions.Count < MinTransactionsForCheck)
                return result;

            var todayRatio = ComputeSpreadRatio(transactions.Select(t => t.CreatedDate).ToList()).Value;
            var baselineRatio = historicalDailyRatios.Count >= MinHistoricalDaysForPersonalBaseline
                ? historicalDailyRatios.Average()
                : DefaultBaselineRatio;

            result.TodayRatio = todayRatio;
            result.BaselineRatio = baselineRatio;
            result.IsBulkEntrySuspected = todayRatio < baselineRatio * BaselineDeviationFactor;
            return result;
        }
    }
}
