using BaskentEnerji.Entity.Entities.ExchangeOffice.Party;
using System;
using System.ComponentModel.DataAnnotations;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Party
{
    public class rm_party
    {
        [Required]
        [StringLength(20)]
        public string PartyCode { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        
        public PartyType Type { get; set; } = PartyType.Both;
        
        [StringLength(100)]
        public string? ContactPerson { get; set; }
        
       
        [StringLength(100)]
        public string? Email { get; set; }
        
        
        public string? Phone { get; set; }
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        [StringLength(50)]
        public string? TaxNumber { get; set; }
        
        [StringLength(50)]
        public string? RegistrationNumber { get; set; }
        
        public bool HasCreditLimit { get; set; }
        
        [Range(0, 365)]
        public int DefaultPaymentTermDays { get; set; } = 0;
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        [Required]
        public Guid OfficeId { get; set; }
        
        
        
    }
}