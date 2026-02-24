using System;
using System.Collections.Generic;

namespace AnasıTAS_Deniz.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_partybalance
    {
        public Guid PartyId { get; set; }
        public string PartyCode { get; set; }
        public string PartyName { get; set; }
        public string PartyType { get; set; }
        public List<vm_currencybalance> CurrencyBalances { get; set; } = new List<vm_currencybalance>();
        public decimal TotalReceivables { get; set; }
        public decimal TotalPayables { get; set; }
        public decimal NetBalance { get; set; }
        public decimal TotalReceivablesInBaseCurrency { get; set; }
        public decimal TotalPayablesInBaseCurrency { get; set; }
        public decimal NetBalanceInBaseCurrency { get; set; }
        public Dictionary<string, decimal> BalancesByCurrency { get; set; } = new Dictionary<string, decimal>();
        public DateTime? LastTransactionDate { get; set; }
        public DateTime AsOfDate { get; set; }
    }

    public class vm_currencybalance
    {
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        public decimal Balance { get; set; }
        public decimal BlockedAmount { get; set; }
        public decimal AvailableBalance { get; set; }
        public bool IsReceivable => Balance > 0;
        public bool IsPayable => Balance < 0;
        public decimal? CreditLimit { get; set; }
        public decimal? AvailableCredit { get; set; }
        public DateTime? LastTransactionDate { get; set; }
    }
}