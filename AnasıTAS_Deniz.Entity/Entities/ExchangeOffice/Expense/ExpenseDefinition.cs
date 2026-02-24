using System;
using System.Collections.Generic;

namespace AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Expense
{
    public class ExpenseDefinition : BaseEntity
    {
        public Guid OfficeId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public ExpenseCategory Category { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsRecurring { get; set; } = false;
        public RecurrencePeriod? RecurrencePeriod { get; set; }
        public decimal? DefaultAmount { get; set; }
        public Guid? DefaultCurrencyId { get; set; }
        
        // Navigation properties
        public Office.Office Office { get; set; }
        public Currency.Currency DefaultCurrency { get; set; }
        public ICollection<ExpensePayment> Payments { get; set; }
    }

    public enum ExpenseCategory
    {
        Salary = 1,
        Rent = 2,
        Utilities = 3,
        Office = 4,
        Marketing = 5,
        Travel = 6,
        Insurance = 7,
        Tax = 8,
        Maintenance = 9,
        Other = 10
    }

    public enum RecurrencePeriod
    {
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Quarterly = 4,
        Yearly = 5
    }
}