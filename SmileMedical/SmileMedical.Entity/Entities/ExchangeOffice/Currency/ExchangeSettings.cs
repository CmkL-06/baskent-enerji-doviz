using System;

namespace SmileMedical.Entity.Entities.ExchangeOffice.Currency
{
    /// <summary>
    /// Otomatik kur güncelleme ayarları
    /// </summary>
    public class ExchangeSettings : BaseEntity
    {
        // Otomatik güncelleme ayarları
        public bool IsAutoUpdateEnabled { get; set; } = false;
        public int UpdateIntervalMinutes { get; set; } = 30;
        public int StartHour { get; set; } = 9;
        public int EndHour { get; set; } = 18;
        public string? WorkDays { get; set; } // JSON: ["monday","tuesday","wednesday","thursday","friday"]

        // Kar marjları
        public decimal TryBasedMarginPercent { get; set; } = 5.0m;
        public decimal CrossFiatMarginPercent { get; set; } = 3.0m;
        public decimal CryptoMarginPercent { get; set; } = 2.0m;

        // Dövize özel kar marjları (JSON: {"USD": 5.0, "EUR": 3.0, ...})
        public string? CurrencySpecificMargins { get; set; }

        // Veri kaynakları (aktif/pasif)
        public bool UseTcmb { get; set; } = true;
        public bool UseDovizCom { get; set; } = true;
        public bool UseBinance { get; set; } = true;

        // Strateji seçimi
        /// <summary>
        /// BEST_BUY: En iyi alış/satış fiyatları
        /// AVERAGE: Ortalama fiyatlar
        /// COMPETITIVE: Rekabetçi fiyatlandırma
        /// </summary>
        public string? RateSelectionStrategy { get; set; } = "BEST_BUY";

        // Uyarı eşikleri
        public decimal MaxPriceChangePercent { get; set; } = 5.0m;
        public bool RequireApprovalAboveThreshold { get; set; } = true;

        // Bildirim ayarları
        public string? NotificationEmails { get; set; } // JSON: ["admin@example.com"]
        public bool SendMobileNotifications { get; set; } = false;

        // Son güncelleme
        public DateTime? LastAutoUpdate { get; set; }
    }
}
