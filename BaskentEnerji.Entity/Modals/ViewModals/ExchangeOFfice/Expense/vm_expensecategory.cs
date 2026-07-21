using System;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Expense
{
    public class vm_expensecategory
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public int UsageCount { get; set; } // ExpenseDefinition + ExpenseBudget referans sayısı — silme koruması için frontend'e ipucu
    }
}
