using System;
using System.Collections.Generic;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_intereststatement
    {
        public Guid PartyId { get; set; }
        public string PartyName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalInterest { get; set; }
        public List<vm_interestdetail> Details { get; set; }
    }

    public class vm_interestdetail
    {
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Principal { get; set; }
        public decimal InterestRate { get; set; }
        public int Days { get; set; }
        public decimal InterestAmount { get; set; }
        public DateTime CalculationDate { get; set; }
    }
}