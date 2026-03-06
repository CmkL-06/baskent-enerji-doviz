using System;

namespace SmileMedical.Entity.Entities.ExchangeOffice.Office
{
    /// <summary>
    /// Represents individual vault balance record within a snapshot
    /// Stores the exact balance and reserved amount at the time of snapshot
    /// </summary>
    public class VaultBalanceSnapshotDetail : BaseEntity
    {
        public Guid SnapshotId { get; set; }
        public Guid VaultId { get; set; }
        public Guid CurrencyId { get; set; }
        public decimal Balance { get; set; }
        public decimal ReservedAmount { get; set; }

        // Navigation properties
        public VaultBalanceSnapshot Snapshot { get; set; }
        public Vault Vault { get; set; }
        public Currency.Currency Currency { get; set; }
    }
}
