using System;
using System.Collections.Generic;

namespace SmileMedical.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_ghostpartybalance
    {
        public Guid PartyId { get; set; }
        public string PartyName { get; set; }
        public string PartyCode { get; set; }
        public List<GhostCurrencyBalance> CurrencyBalances { get; set; }
        public decimal TotalBalanceInBaseCurrency { get; set; }
        public decimal TotalBlockedInBaseCurrency { get; set; }
        public decimal TotalAvailableInBaseCurrency { get; set; }
        public DateTime? LastActivityDate { get; set; }
    }

    public class GhostCurrencyBalance
    {
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        public decimal Balance { get; set; }
        public decimal BlockedAmount { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public int TransactionCount { get; set; }
        public DateTime? LastTransactionDate { get; set; }
    }
}