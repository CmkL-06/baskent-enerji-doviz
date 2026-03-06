using System;
using System.Collections.Generic;

namespace SmileMedical.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_ghostpartysummary
    {
        public Guid PartyId { get; set; }
        public string PartyName { get; set; }
        public string PartyCode { get; set; }
        public string PartyType { get; set; }
        public int TotalAccounts { get; set; }
        public int ActiveAccounts { get; set; }
        public decimal TotalVolumeInBaseCurrency { get; set; }
        public DateTime? FirstTransactionDate { get; set; }
        public DateTime? LastTransactionDate { get; set; }
        public List<vm_ghostpartyaccount> Accounts { get; set; }
        public Dictionary<string, decimal> TotalBalancesByCurrency { get; set; }
        public Dictionary<string, int> TransactionCountByCurrency { get; set; }
    }
}