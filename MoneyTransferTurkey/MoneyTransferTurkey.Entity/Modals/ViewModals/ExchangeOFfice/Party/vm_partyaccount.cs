using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Party;
using System;

namespace MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_partyaccount
    {
        public Guid Id { get; set; }
        public Guid PartyId { get; set; }
        public string PartyCode { get; set; }
        public string PartyName { get; set; }
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        public string AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public decimal BlockedAmount { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public int TransactionCount { get; set; }
        public DateTime? LastTransactionDate { get; set; }
        public AccountStatus Status { get; set; }
        public string StatusName { get; set; }
        public DateTime? StatusChangedDate { get; set; }
        public string StatusReason { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastActivityDate { get; set; }
        
        // Analysis fields
        public bool IsReceivable => Balance > 0;
        public bool IsPayable => Balance < 0;
        public decimal AbsoluteBalance => Math.Abs(Balance);
        public string BalanceType => Balance > 0 ? "Receivable" : Balance < 0 ? "Payable" : "Zero";
        
        // Credit information if applicable
        public bool HasCreditLimit { get; set; }
        public decimal? CreditLimit { get; set; }
        public decimal? AvailableCredit { get; set; }
        public decimal? CreditUtilization { get; set; }
    }
}