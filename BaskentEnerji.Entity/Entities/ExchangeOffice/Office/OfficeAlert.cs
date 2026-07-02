namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Office
{
    public class OfficeAlert : BaseEntity
    {
        public Guid OfficeId { get; set; }
        public Office Office { get; set; }

        public AlertType AlertType { get; set; }
        public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;

        public string Title { get; set; }
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public Guid? ReadByUserId { get; set; }

        public bool IsResolved { get; set; } = false;
        public DateTime? ResolvedAt { get; set; }

        // Uyarıyı tetikleyen referans (ör. transferId, vaultBalanceId)
        public string? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
    }
}
