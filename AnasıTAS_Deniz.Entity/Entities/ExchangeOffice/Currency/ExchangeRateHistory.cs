using System;

namespace AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Currency
{
    /// <summary>
    /// Kur değişiklik geçmişi - Her güncelleme kaydedilir
    /// </summary>
    public class ExchangeRateHistory : BaseEntity
    {
        public Guid? OfficeId { get; set; } // null = tüm ofisler
        public Guid SourceCurrencyId { get; set; }
        public Guid TargetCurrencyId { get; set; }

        // Eski değerler
        public decimal? OldBuyRate { get; set; }
        public decimal? OldSellRate { get; set; }

        // Yeni değerler
        public decimal NewBuyRate { get; set; }
        public decimal NewSellRate { get; set; }

        // Değişim yüzdesi
        public decimal ChangePercent { get; set; }

        // Güncelleme kaynağı
        /// <summary>
        /// MANUAL: Kullanıcı manuel girdi
        /// AUTO_SYSTEM: Otomatik sistem güncellemesi
        /// </summary>
        public string? UpdateSource { get; set; }

        // Veri kaynakları (JSON array)
        /// <summary>
        /// ["tcmb", "harem", "binance"] gibi
        /// </summary>
        public string? DataSources { get; set; }

        // Kim güncelledi (null = system)
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }

        // Onay durumu
        public bool IsApproved { get; set; } = true;

        // Navigation properties
        public virtual Office.Office? Office { get; set; }
        public virtual Currency SourceCurrency { get; set; }
        public virtual Currency TargetCurrency { get; set; }
    }
}
