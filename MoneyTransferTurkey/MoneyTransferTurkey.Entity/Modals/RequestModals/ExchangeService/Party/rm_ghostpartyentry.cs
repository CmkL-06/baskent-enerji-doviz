using System;
using System.ComponentModel.DataAnnotations;

namespace MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Party
{
    public class rm_ghostpartyentry
    {
        [Required]
        public Guid GhostAccountId { get; set; }

        [Required]
        public Guid OfficeId { get; set; }

        [Required]
        public Guid CurrencyId { get; set; }

        [Required]
        public Guid VaultId { get; set; }

        [Required]
        [MaxLength(20)]
        public string EntryType { get; set; } // "Debit" or "Credit"

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [MaxLength(50)]
        public string ReferenceNumber { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        [MaxLength(50)]
        public string PaymentMethod { get; set; }

        [MaxLength(500)]
        public string Note { get; set; }

        [Required]
        public Guid CreatedBy { get; set; }
    }
}