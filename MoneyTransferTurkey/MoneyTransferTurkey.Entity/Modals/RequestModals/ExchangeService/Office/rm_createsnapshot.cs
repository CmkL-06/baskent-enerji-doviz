using System;

namespace MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Office
{
    /// <summary>
    /// Request model for creating a vault balance snapshot
    /// </summary>
    public class rm_createsnapshot
    {
        public Guid OfficeId { get; set; }
        public string? Description { get; set; }
    }
}
