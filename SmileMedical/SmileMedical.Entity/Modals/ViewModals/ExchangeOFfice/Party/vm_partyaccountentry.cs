using SmileMedical.Entity.Entities.ExchangeOffice.Party;
using System;

namespace SmileMedical.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_partyaccountentry
    {
        public Guid Id { get; set; }
        public Guid PartyAccountId { get; set; }
        public string AccountNumber { get; set; }
        public string PartyCode { get; set; }
        public string PartyName { get; set; }
        public string CurrencyCode { get; set; }
        public Guid? TransactionId { get; set; }
        public string TransactionNumber { get; set; }
        public string EntryNumber { get; set; }
        public EntryType Type { get; set; }
        public string TypeName { get; set; }
        public decimal Amount { get; set; }
        public decimal DebitAmount => Type == EntryType.Debit ? Math.Abs(Amount) : 0;
        public decimal CreditAmount => Type == EntryType.Credit ? Math.Abs(Amount) : 0;
        public decimal RunningBalance { get; set; }
        public string Description { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime? DueDate { get; set; }
        public int? DaysOverdue => DueDate.HasValue && DueDate < DateTime.UtcNow ? 
            (int)(DateTime.UtcNow - DueDate.Value).TotalDays : null;
        public PaymentStatus PaymentStatus { get; set; }
        public string PaymentStatusName { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentReference { get; set; }
        public bool IsReconciled { get; set; }
        public DateTime? ReconciledDate { get; set; }
        public string ReconciledByUserName { get; set; }
        public DateTime CreatedDate { get; set; }
        //public string CreatedByUserName { get; set; }
        public bool IsReversed { get; set; }
        public Guid? ReversalEntryId { get; set; }
        public string ReversalEntryNumber { get; set; }

        // Orijinal döviz bilgileri
        public Guid? OriginalCurrencyId { get; set; }
        public string OriginalCurrencyCode { get; set; }
        public decimal? OriginalAmount { get; set; }
        public decimal? ExchangeRate { get; set; }
        public bool IsCustomRate { get; set; }
        public string FormattedOriginalAmount =>
            OriginalAmount.HasValue && !string.IsNullOrEmpty(OriginalCurrencyCode)
                ? $"{(Type == EntryType.Credit ? "+" : "-")}{OriginalAmount:F2} {OriginalCurrencyCode}"
                : null;
    }
}