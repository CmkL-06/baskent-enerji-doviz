using System;

namespace AnasıTAS_Deniz.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_ghostpartystatement
    {
        public DateTime Date { get; set; }
        public string ReferenceNumber { get; set; }
        public string Description { get; set; }
        public string EntryType { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public decimal RunningBalance { get; set; }
        public string CurrencyCode { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public string Note { get; set; }
    }
}