namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Expense
{
    public class vm_expensebudgethistory
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Label { get; set; } // e.g. "Tem 2026", built server-side for convenience
        public decimal TotalBudget { get; set; }
        public decimal TotalActual { get; set; }
    }
}
