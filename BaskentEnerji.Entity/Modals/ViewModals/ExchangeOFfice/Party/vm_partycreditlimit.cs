using System;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_partycreditlimit
    {
        public Guid Id { get; set; }
        public Guid PartyId { get; set; }
        public string PartyCode { get; set; }
        public string PartyName { get; set; }
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal UtilizedAmount { get; set; }
        public decimal AvailableCredit { get; set; }
        public decimal UtilizationPercentage => CreditLimit > 0 ? (UtilizedAmount / CreditLimit) * 100 : 0;
        public int PaymentTermDays { get; set; }
        public decimal? InterestRate { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; }
        public string Status => IsActive ? "Active" : "Inactive";
        public DateTime ApprovedDate { get; set; }
        public string ApprovedByUserName { get; set; }
        public string ApprovalNotes { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal AvailableAmount => AvailableCredit;
        public DateTime LastReviewDate { get; set; }
    }
}