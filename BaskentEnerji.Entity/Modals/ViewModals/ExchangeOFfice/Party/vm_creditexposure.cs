using System;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_creditexposure
    {
        public Guid PartyId { get; set; }
        public string PartyCode { get; set; }
        public string PartyName { get; set; }
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal CurrentExposure { get; set; }
        public decimal AvailableCredit { get; set; }
        public decimal ExposurePercentage { get; set; }
        public int DaysOverdue { get; set; }
        public decimal OverdueAmount { get; set; }
        public string RiskLevel { get; set; }
    }
}