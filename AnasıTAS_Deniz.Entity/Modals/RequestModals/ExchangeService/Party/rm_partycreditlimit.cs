using System;
using System.ComponentModel.DataAnnotations;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.ExchangeService.Party
{
    public class rm_partycreditlimit
    {
        [Required]
        public Guid PartyId { get; set; }
        
        [Required]
        public Guid CurrencyId { get; set; }
        
        [Required]
        [Range(0, double.MaxValue)]
        public decimal CreditLimit { get; set; }
        
        [Required]
        [Range(0, 365)]
        public int PaymentTermDays { get; set; }
        
        [Range(0, 100)]
        public decimal? InterestRate { get; set; }
        
        [Required]
        public DateTime EffectiveFrom { get; set; }
        
        public DateTime? EffectiveTo { get; set; }
        
        [StringLength(1000)]
        public string Notes { get; set; }
        
        [Required]
        public Guid ApprovedByUserId { get; set; }
    }
}