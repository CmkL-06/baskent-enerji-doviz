using AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Party;
using System;
using System.ComponentModel.DataAnnotations;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.ExchangeService.Party
{
    public class rm_partyaccountentry
    {
        [Required]
        public Guid PartyAccountId { get; set; }
        
        public Guid? TransactionId { get; set; }
        
        [Required]
        public EntryType EntryType { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; }
        
        [StringLength(100)]
        public string ReferenceNumber { get; set; }
        
        [Required]
        public DateTime EntryDate { get; set; }
        
        public DateTime? DueDate { get; set; }
        
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        
        [StringLength(100)]
        public string PaymentReference { get; set; }
        
        [Required]
        public Guid CreatedByUserId { get; set; }
    }
}