using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Office
{
    public class Transaction : BaseEntity
    {
        public string TransactionNumber { get; set; } // Unique transaction ID
        public Guid VaultId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? PartyId { get; set; } // Link to Party for account tracking
        public Guid UserId { get; set; } // Employee who processed
        public TransactionType Type { get; set; }
        public DateTime TransactionDate { get; set; }
        public TransactionStatus Status { get; set; }
        public string? Notes { get; set; }
        public decimal Profit { get; set; }
       
        public bool IsCustomRate { get; set; } // Flag to indicate if custom rate was used

        public string? DeletedReason { get; set; }
        public bool IsDeleted { get; set; }
        public Guid? deletedByUserId { get; set; }

        // Navigation properties
        public Vault Vault { get; set; }
        public User.User User { get; set; }
        public Party.Party Party { get; set; }
        public ICollection<TransactionDetail> Details { get; set; }
        public ICollection<Party.PartyAccountEntry> PartyAccountEntries { get; set; }
    }

    public enum TransactionType
    {
        Exchange = 1,      // Currency exchange
        Deposit = 2,       // Vault deposit
        Withdrawal = 3,    // Vault withdrawal
        Transfer = 4,      // Inter-vault transfer
        Adjustment = 5,     // Manual adjustment
        Party = 6,
    }

    public enum TransactionStatus
    {
        Pending = 1,
        Completed = 2,
        Cancelled = 3,
        Failed = 4
    }
}