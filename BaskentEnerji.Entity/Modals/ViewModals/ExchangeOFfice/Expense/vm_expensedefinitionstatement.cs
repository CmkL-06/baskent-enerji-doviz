using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using System;
using System.Collections.Generic;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Expense
{
    public class vm_expensedefinitionstatement
    {
        public Guid DefinitionId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string? AccountReference { get; set; }
        public bool IsRecurring { get; set; }
        public RecurrencePeriod? RecurrencePeriod { get; set; }
        public string? RecurrencePeriodName { get; set; }
        public DateTime? NextDueDate { get; set; }
        public string? DueStatus { get; set; }
        public decimal TotalPaid { get; set; }
        public int PaymentCount { get; set; }
        public decimal AverageAmount { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public List<vm_expensedefinitionstatementline> Lines { get; set; } = new();
    }

    public class vm_expensedefinitionstatementline
    {
        public Guid PaymentId { get; set; }
        public string PaymentNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public decimal RunningTotal { get; set; }
        public string CurrencyCode { get; set; }
        public string StatusName { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Description { get; set; }
    }
}
