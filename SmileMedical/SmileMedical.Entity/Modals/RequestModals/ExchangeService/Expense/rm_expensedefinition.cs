using SmileMedical.Entity.Entities.ExchangeOffice.Expense;
using System;
using System.ComponentModel.DataAnnotations;

namespace SmileMedical.Entity.Modals.RequestModals.ExchangeService.Expense
{
    public class rm_expensedefinition
    {
        public Guid? Id { get; set; }
        
        [Required]
        public Guid OfficeId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Code { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        
        [Required]
        public ExpenseCategory Category { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool IsRecurring { get; set; } = false;
        
        public RecurrencePeriod? RecurrencePeriod { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal? DefaultAmount { get; set; }
        
        public Guid? DefaultCurrencyId { get; set; }
    }
}