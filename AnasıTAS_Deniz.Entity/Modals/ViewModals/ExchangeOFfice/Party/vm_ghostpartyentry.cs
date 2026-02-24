using System;

namespace AnasıTAS_Deniz.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_ghostpartyentry
    {
        public Guid Id { get; set; }
        public Guid GhostAccountId { get; set; }
        public Guid PartyId { get; set; }
        public string PartyName { get; set; }
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; }
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public string EntryType { get; set; }
        public decimal Amount { get; set; }
        public decimal RunningBalance { get; set; }
        public string ReferenceNumber { get; set; }
        public string Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public Guid? TransactionId { get; set; }
        public Guid? VaultId { get; set; }
        public string VaultName { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public bool IsReconciled { get; set; }
        public DateTime? ReconciledDate { get; set; }
        public Guid? ReconciledBy { get; set; }
        public string Note { get; set; }
        public Guid CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}