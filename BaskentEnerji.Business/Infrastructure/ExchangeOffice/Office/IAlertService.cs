using BaskentEnerji.Entity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office
{
    public class vm_alert
    {
        public Guid Id { get; set; }
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; } = "";
        public string AlertType { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public bool IsRead { get; set; }
        public bool IsResolved { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
    }

    public interface IAlertService
    {
        Task<List<vm_alert>> GetUnreadAlertsAsync();
        Task<List<vm_alert>> GetAlertsByOfficeAsync(Guid officeId, int limit = 50);
        Task MarkAsReadAsync(Guid alertId, Guid userId);
        Task MarkAllAsReadAsync(Guid userId);
        Task ResolveAlertAsync(Guid alertId);
        Task CreateAlertAsync(Guid officeId, AlertType type, AlertSeverity severity, string title, string message, string? referenceId = null, string? referenceType = null);
        Task CheckLowBalancesAsync(decimal threshold = 100);
        Task CheckStaffDailyAnomaliesAsync(DateTime businessDate);
    }
}
