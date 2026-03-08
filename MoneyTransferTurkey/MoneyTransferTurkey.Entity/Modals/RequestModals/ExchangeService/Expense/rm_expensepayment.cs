using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Expense;
using System;
using System.ComponentModel.DataAnnotations;

namespace MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Expense
{
    public class rm_expensepayment
    {
        [Required]
        public Guid ExpenseDefinitionId { get; set; }
        
        [Required]
        public Guid CurrencyId { get; set; }
        
        [Required]
        public DateTime PaymentDate { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
        
        [Required]
        public PaymentMethod PaymentMethod { get; set; }
        
        [StringLength(100)]
        public string? ReferenceNumber { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public string? Receipt { get; set; }
    }
}