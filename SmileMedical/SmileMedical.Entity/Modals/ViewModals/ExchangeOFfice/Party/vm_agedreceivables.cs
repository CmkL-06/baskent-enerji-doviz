using System;
using System.Collections.Generic;

namespace SmileMedical.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_agedreceivables
    {
        public Guid PartyId { get; set; }
        public string PartyCode { get; set; }
        public string PartyName { get; set; }
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal Current { get; set; }
        public decimal Days1To30 { get; set; }
        public decimal Days31To60 { get; set; }
        public decimal Days61To90 { get; set; }
        public decimal Over90Days { get; set; }
        public DateTime OldestInvoiceDate { get; set; }
        public int DaysOutstanding { get; set; }
        public List<vm_agedinvoice> Details { get; set; }
    }

    public class vm_agedinvoice
    {
        public Guid EntryId { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal Amount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public int DaysOverdue { get; set; }
        public string Description { get; set; }
    }
}