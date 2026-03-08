using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Party
{
    [Table("GhostPartyAccountEntries")]
    public class GhostPartyAccountEntry : BaseEntity
    {
        [Required]
        public Guid GhostAccountId { get; set; }

        [Required]
        public Guid OfficeId { get; set; }

        [Required]
        public Guid CurrencyId { get; set; }

        [Required]
        [MaxLength(20)]
        public string EntryType { get; set; } // "Debit" or "Credit"

        [Column(TypeName = "decimal(18,4)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal RunningBalance { get; set; }

        [MaxLength(50)]
        public string ReferenceNumber { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public Guid? TransactionId { get; set; }

        public Guid? VaultId { get; set; }

        [MaxLength(50)]
        public string PaymentMethod { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Completed";

        public bool IsReconciled { get; set; } = false;

        public DateTime? ReconciledDate { get; set; }

        public Guid? ReconciledBy { get; set; }

        [MaxLength(500)]
        public string Note { get; set; }

        public Guid CreatedBy { get; set; }

        public Guid? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [ForeignKey("GhostAccountId")]
        public virtual GhostPartyAccount GhostAccount { get; set; }

        [ForeignKey("OfficeId")]
        public virtual Office.Office Office { get; set; }

        [ForeignKey("CurrencyId")]
        public virtual Currency.Currency Currency { get; set; }

        [ForeignKey("TransactionId")]
        public virtual Office.Transaction Transaction { get; set; }

        [ForeignKey("VaultId")]
        public virtual Office.Vault Vault { get; set; }
    }
}