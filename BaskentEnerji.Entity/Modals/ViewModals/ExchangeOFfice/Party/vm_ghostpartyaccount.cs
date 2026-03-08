using System;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_ghostpartyaccount
    {
        public Guid Id { get; set; }
        public Guid PartyId { get; set; }
        public string PartyName { get; set; }
        public string PartyCode { get; set; }
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; }
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
        public string Note { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}