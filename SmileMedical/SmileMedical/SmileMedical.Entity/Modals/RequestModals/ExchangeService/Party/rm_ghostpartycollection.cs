using System;
using System.ComponentModel.DataAnnotations;

namespace SmileMedical.Entity.Modals.RequestModals.ExchangeService.Party
{
    public class rm_ghostpartycollection
    {
        [Required]
        public Guid PartyId { get; set; }

        [Required]
        public Guid OfficeId { get; set; }

        [Required]
        public Guid CurrencyId { get; set; }

        [Required]
        public Guid VaultId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [MaxLength(50)]
        public string ReferenceNumber { get; set; }

        [Required]
        [MaxLength(500)]
        public string Description { get; set; }

        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Cash";

        [MaxLength(500)]
        public string Note { get; set; }

        [Required]
        public Guid CreatedBy { get; set; }
    }
}