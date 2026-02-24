using AnasıTAS_Deniz.Entity.Entities;
using AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Office;
using AnasıTAS_Deniz.Entity.Modals.ViewModals.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_transaction : BaseEntity
    {
        public string TransactionNumber { get; set; } // Unique transaction ID
        public Guid VaultId { get; set; }
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid UserId { get; set; } // Employee who processed
        public TransactionType Type { get; set; }
        public DateTime TransactionDate { get; set; }
        public TransactionStatus Status { get; set; }
        public string Notes { get; set; }

        public bool IsCustomRate { get; set; }
        public decimal Profit { get; set; }

        public string DeletedReason { get; set; }
        public bool IsDeleted { get; set; }
        public string DeletedBy { get; set; }

        // Navigation properties
        public string VaultName { get; set; }

        public vm_user User { get; set; }
        public ICollection<vm_transactiondetail> Details { get; set; }

    }
}
