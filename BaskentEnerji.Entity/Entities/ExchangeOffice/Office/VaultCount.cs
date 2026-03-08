using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using System;
using System.Collections.Generic;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Office
{
    public class VaultCount : BaseEntity
    {
        public Guid VaultId { get; set; }
        public Guid OfficeId { get; set; }
        public Guid UserId { get; set; }
        public DateTime CountDate { get; set; }
        public bool HasDiscrepancy { get; set; }
        public string DiscrepancyDetails { get; set; }
        public bool IsSystemGenerated { get; set; } // false if manually triggered

        public Vault Vault { get; set; }
        public Office Office { get; set; }
        public ICollection<VaultCountDetail> CountDetails { get; set; }
    }

    public class VaultCountDetail : BaseEntity
    {
        public Guid VaultCountId { get; set; }
        public Guid CurrencyId { get; set; }
        public decimal ActualAmount { get; set; } // Amount entered by employee
        public decimal SystemAmount { get; set; } // Amount that should be in vault according to system
        public decimal Discrepancy { get; set; } // Difference between actual and system

        public VaultCount VaultCount { get; set; }
        public Currency.Currency Currency { get; set; }
    }
}