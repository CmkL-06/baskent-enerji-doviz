using System;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Currency
{
    /// <summary>
    /// Bekleyen kur onayları - Anormal değişimler için
    /// </summary>
    public class PendingRateApproval : BaseEntity
    {
        public Guid? OfficeId { get; set; } // null = tüm ofisler
        public Guid SourceCurrencyId { get; set; }
        public Guid TargetCurrencyId { get; set; }

        // Mevcut kurlar
        public decimal CurrentBuyRate { get; set; }
        public decimal CurrentSellRate { get; set; }

        // Önerilen kurlar
        public decimal ProposedBuyRate { get; set; }
        public decimal ProposedSellRate { get; set; }

        // Değişim yüzdesi
        public decimal ChangePercent { get; set; }

        // Neden onay gerekiyor
        public string? Reason { get; set; }

        // Kaynak veriler (JSON - detaylı bilgi)
        public string? SourceData { get; set; }

        // Durum
        /// <summary>
        /// PENDING: Beklemede
        /// APPROVED: Onaylandı
        /// REJECTED: Reddedildi
        /// </summary>
        public string? Status { get; set; } = "PENDING";

        // Onay bilgileri
        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalNotes { get; set; }

        // Navigation properties
        public virtual Office.Office? Office { get; set; }
        public virtual Currency SourceCurrency { get; set; }
        public virtual Currency TargetCurrency { get; set; }
    }
}
