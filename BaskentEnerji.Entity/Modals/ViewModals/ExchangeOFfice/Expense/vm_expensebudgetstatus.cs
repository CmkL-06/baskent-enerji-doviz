using System;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Expense
{
    public class vm_expensebudgetstatus
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal BudgetAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal PercentUsed { get; set; } // 0 if no budget set (guarded divide-by-zero)
        public string StatusLevel { get; set; } // "green" | "amber" | "red" | "none" (no budget set)
    }
}
