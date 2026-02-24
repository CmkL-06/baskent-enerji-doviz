using System;

namespace AnasıTAS_Deniz.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_creditavailability
    {
        public Guid PartyId { get; set; }
        public string PartyCode { get; set; }
        public string PartyName { get; set; }
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public decimal RequestedAmount { get; set; }
        public bool HasCreditLimit { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal UtilizedAmount { get; set; }
        public decimal AvailableCredit { get; set; }
        public bool IsApproved { get; set; }
        public string Reason { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal ProjectedBalance { get; set; }
        public int PaymentTermDays { get; set; }
        public DateTime? ExpectedPaymentDate { get; set; }
        public decimal? InterestRate { get; set; }
        public bool RequiresApproval { get; set; }
        public string ApprovalNotes { get; set; }
    }
}