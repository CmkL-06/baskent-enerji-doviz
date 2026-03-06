using System;

namespace SmileMedical.Entity.Entities.ExchangeOffice.Expense
{
    public class ExpensePayment : BaseEntity
    {
        public Guid ExpenseDefinitionId { get; set; }
        public Guid VaultId { get; set; }
        public Guid CurrencyId { get; set; }
        public string PaymentNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Description { get; set; }
        public string? Receipt { get; set; } // File path or URL to receipt
        public ExpenseStatus Status { get; set; } = ExpenseStatus.Paid;
        public bool IsDeleted { get; set; } = false;
        public string? DeletedReason { get; set; }
        public Guid? DeletedByUserId { get; set; }
        public DateTime? DeletedDate { get; set; }
        public Guid CreatedByUserId { get; set; }
        
        // Navigation properties
        public ExpenseDefinition ExpenseDefinition { get; set; }
        public Office.Vault Vault { get; set; }
        public Currency.Currency Currency { get; set; }
        public User.User CreatedByUser { get; set; }
        public User.User DeletedByUser { get; set; }
    }

    public enum PaymentMethod
    {
        Cash = 1,
        BankTransfer = 2,
        CreditCard = 3,
        Check = 4,
        Other = 5
    }

    public enum ExpenseStatus
    {
        Pending = 1,
        Paid = 2,
        Cancelled = 3,
        Refunded = 4
    }
}