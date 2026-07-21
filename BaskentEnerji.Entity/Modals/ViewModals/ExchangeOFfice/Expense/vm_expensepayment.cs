using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using System;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Expense
{
    public class vm_expensepayment
    {
        public Guid Id { get; set; }
        public Guid ExpenseDefinitionId { get; set; }
        public string ExpenseDefinitionName { get; set; }
        public string ExpenseDefinitionCode { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public Guid VaultId { get; set; }
        public string VaultName { get; set; }
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencySymbol { get; set; }
        public string PaymentNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentMethodName { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Description { get; set; }
        public string? Receipt { get; set; }
        public ExpenseStatus Status { get; set; }
        public string StatusName { get; set; }
        public bool IsDeleted { get; set; }
        public string? DeletedReason { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string? DeletedByUserName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedByUserName { get; set; }
        public string? ApprovedByUserName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectionNote { get; set; }
    }
}