using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using System;
// TransferStatus enum: BaskentEnerji.Entity.Enums.cs

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Office
{
    /// <summary>
    /// Merkez → Şube/Bayi arası onaylı para transferi.
    /// Sadece Merkez tipi ofisler transfer gönderebilir; Şube/Bayi talep oluşturur.
    /// </summary>
    public class OfficeTransfer : BaseEntity
    {
        // Transfer edilen kasa bilgileri
        public Guid SourceVaultId { get; set; }
        public Vault SourceVault { get; set; }

        public Guid TargetVaultId { get; set; }
        public Vault TargetVault { get; set; }

        // Para birimi ve miktar
        public Guid CurrencyId { get; set; }
        public Currency.Currency Currency { get; set; }
        public decimal Amount { get; set; }

        // Durum ve akış
        public TransferStatus Status { get; set; } = TransferStatus.Pending;
        public Guid RequestedByUserId { get; set; }
        public User.User RequestedBy { get; set; }

        public Guid? ApprovedByUserId { get; set; }
        public User.User? ApprovedBy { get; set; }

        public string? Notes { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
