using System;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Expense
{
    public class ExpenseBudget : BaseEntity
    {
        public Guid OfficeId { get; set; }
        public Guid CategoryId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; } // 1-12
        public decimal BudgetAmount { get; set; } // Always TRY
        public Guid CreatedByUserId { get; set; }

        // Navigation properties
        public Office.Office Office { get; set; }
        public User.User CreatedByUser { get; set; }
        public ExpenseCategoryDefinition Category { get; set; }
    }
}
