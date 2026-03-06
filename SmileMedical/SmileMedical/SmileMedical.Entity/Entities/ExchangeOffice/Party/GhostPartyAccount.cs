using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmileMedical.Entity.Entities.ExchangeOffice.Party
{
    [Table("GhostPartyAccounts")]
    public class GhostPartyAccount : BaseEntity
    {
        [Required]
        public Guid PartyId { get; set; }

        [Required]
        public Guid OfficeId { get; set; }

        [Required]
        public Guid CurrencyId { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Balance { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal BlockedAmount { get; set; } = 0;

        [NotMapped]
        public decimal AvailableBalance => Balance - BlockedAmount;

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalDebits { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalCredits { get; set; } = 0;

        public int TransactionCount { get; set; } = 0;

        public DateTime? LastTransactionDate { get; set; }

        [MaxLength(500)]
        public string Note { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? UpdatedDate { get; set; }

        [ForeignKey("PartyId")]
        public virtual Party Party { get; set; }

        [ForeignKey("OfficeId")]
        public virtual Office.Office Office { get; set; }

        [ForeignKey("CurrencyId")]
        public virtual Currency.Currency Currency { get; set; }

        public virtual ICollection<GhostPartyAccountEntry> Entries { get; set; }
    }
}