using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using System;
using System.ComponentModel.DataAnnotations;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Expense
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
        public Guid CategoryId { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool IsRecurring { get; set; } = false;
        
        public RecurrencePeriod? RecurrencePeriod { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal? DefaultAmount { get; set; }

        public Guid? DefaultCurrencyId { get; set; }

        [StringLength(100)]
        public string? AccountReference { get; set; }

        [Range(1, 31)]
        public int? DueDayOfMonth { get; set; }
    }
}