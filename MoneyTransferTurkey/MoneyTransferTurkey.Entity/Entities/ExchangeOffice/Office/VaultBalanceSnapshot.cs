using System;
using System.Collections.Generic;

namespace MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Office
{
    /// <summary>
    /// Represents a snapshot of all vault balances at a specific point in time
    /// Used for historical tracking and reporting
    /// </summary>
    public class VaultBalanceSnapshot : BaseEntity
    {
        public Guid OfficeId { get; set; }
        public Guid UserId { get; set; }
        public DateTime SnapshotDate { get; set; }
        public string? Description { get; set; }

        // Navigation properties
        public Office Office { get; set; }
        public ICollection<VaultBalanceSnapshotDetail> Details { get; set; }
    }
}
