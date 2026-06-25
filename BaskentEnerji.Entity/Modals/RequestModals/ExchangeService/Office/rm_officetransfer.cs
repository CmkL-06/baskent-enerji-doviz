using System;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office
{
    /// <summary>Transfer talebi oluşturma isteği.</summary>
    public class rm_create_officetransfer
    {
        public Guid SourceVaultId { get; set; }
        public Guid TargetVaultId { get; set; }
        public Guid CurrencyId { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>Transfer onay/red isteği (Merkez kullanır).</summary>
    public class rm_action_officetransfer
    {
        public bool Approve { get; set; }
        public string? RejectionReason { get; set; }
    }
}
