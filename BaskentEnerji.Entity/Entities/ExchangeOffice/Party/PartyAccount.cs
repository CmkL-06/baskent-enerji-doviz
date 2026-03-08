using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Party
{
    public class PartyAccount : BaseEntity
    {
        public Guid PartyId { get; set; }
        public Guid CurrencyId { get; set; }
        public string AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public decimal BlockedAmount { get; set; }
        public decimal CreditLimit { get; set; }
        public int PaymentTermDays { get; set; } = 0;
        public DateTime? LastActivityDate { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public int TransactionCount { get; set; }
        public DateTime? LastTransactionDate { get; set; }
        public AccountStatus Status { get; set; } = AccountStatus.Active;
        public bool IsActive { get; set; } = true;

        // Computed property
        public decimal AvailableBalance => Balance - BlockedAmount;

        // Navigation properties
        public Party Party { get; set; }
        public Currency.Currency Currency { get; set; }
        public ICollection<PartyAccountEntry> Entries { get; set; }
    }

    public enum AccountStatus
    {
        Active = 1,
        Inactive = 2,
        Suspended = 3,
        Closed = 4
    }
}