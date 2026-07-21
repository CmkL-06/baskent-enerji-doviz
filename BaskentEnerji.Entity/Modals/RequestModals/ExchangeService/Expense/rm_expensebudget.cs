using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using System;
using System.ComponentModel.DataAnnotations;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Expense
{
    public class rm_expensebudget
    {
        [Required]
        public Guid OfficeId { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        [Required]
        [Range(2000, 2100)]
        public int Year { get; set; }

        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal BudgetAmount { get; set; }
    }
}
