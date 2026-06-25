using System;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_officetransfer
    {
        public Guid Id { get; set; }
        public DateTime CreatedDate { get; set; }

        public Guid SourceVaultId { get; set; }
        public string SourceVaultName { get; set; }
        public string SourceOfficeName { get; set; }

        public Guid TargetVaultId { get; set; }
        public string TargetVaultName { get; set; }
        public string TargetOfficeName { get; set; }

        public string CurrencyCode { get; set; }
        public decimal Amount { get; set; }

        public string Status { get; set; }
        public string? Notes { get; set; }
        public string? RejectionReason { get; set; }

        public string RequestedByName { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
