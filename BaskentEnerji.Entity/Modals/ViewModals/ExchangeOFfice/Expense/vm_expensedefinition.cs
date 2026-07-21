using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using System;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Expense
{
    public class vm_expensedefinition
    {
        public Guid Id { get; set; }
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsRecurring { get; set; }
        public RecurrencePeriod? RecurrencePeriod { get; set; }
        public string? RecurrencePeriodName { get; set; }
        public decimal? DefaultAmount { get; set; }
        public Guid? DefaultCurrencyId { get; set; }
        public string? DefaultCurrencyCode { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal TotalPayments { get; set; }
        public int PaymentCount { get; set; }
        public string? AccountReference { get; set; }
        public int? DueDayOfMonth { get; set; }
        public DateTime? NextDueDate { get; set; }
        public string? DueStatus { get; set; }
    }
}