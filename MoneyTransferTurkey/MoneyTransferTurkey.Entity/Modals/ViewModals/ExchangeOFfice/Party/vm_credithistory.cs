using System;
using System.Collections.Generic;

namespace MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_credithistory
    {
        public Guid PartyId { get; set; }
        public string PartyName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<vm_credithistoryentry> Entries { get; set; }
    }

    public class vm_credithistoryentry
    {
        public DateTime Date { get; set; }
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public string Action { get; set; }
        public decimal Amount { get; set; }
        public decimal CreditLimitBefore { get; set; }
        public decimal CreditLimitAfter { get; set; }
        public decimal UtilizedBefore { get; set; }
        public decimal UtilizedAfter { get; set; }
        public string ReferenceNumber { get; set; }
        public string Notes { get; set; }
        public string UserName { get; set; }
    }
}