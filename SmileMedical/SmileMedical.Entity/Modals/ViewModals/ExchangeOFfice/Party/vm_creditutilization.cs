using System;
using System.Collections.Generic;

namespace SmileMedical.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_creditutilization
    {
        public Guid PartyId { get; set; }
        public string PartyName { get; set; }
        public decimal TotalCreditLimit { get; set; }
        public decimal TotalUtilized { get; set; }
        public decimal TotalAvailable { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public List<vm_creditutilizationbycurrency> CurrencyBreakdown { get; set; }
    }

    public class vm_creditutilizationbycurrency
    {
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal Utilized { get; set; }
        public decimal Available { get; set; }
        public decimal UtilizationPercentage { get; set; }
    }
}