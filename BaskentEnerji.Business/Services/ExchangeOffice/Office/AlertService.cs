using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Office
{
    public class AlertService : IAlertService
    {
        private readonly BaskentEnerjiDbContext _db;

        public AlertService(BaskentEnerjiDbContext db)
        {
            _db = db;
        }

        public async Task<List<vm_alert>> GetUnreadAlertsAsync()
        {
            var alerts = await _db.OfficeAlerts
                .AsNoTracking()
                .Include(a => a.Office)
                .Where(a => !a.IsRead && !a.IsResolved)
                .OrderByDescending(a => a.CreatedDate)
                .Take(100)
                .ToListAsync();

            return alerts.Select(MapToVm).ToList();
        }

        public async Task<List<vm_alert>> GetAlertsByOfficeAsync(Guid officeId, int limit = 50)
        {
            var alerts = await _db.OfficeAlerts
                .AsNoTracking()
                .Include(a => a.Office)
                .Where(a => a.OfficeId == officeId)
                .OrderByDescending(a => a.CreatedDate)
                .Take(limit)
                .ToListAsync();

            return alerts.Select(MapToVm).ToList();
        }

        public async Task MarkAsReadAsync(Guid alertId, Guid userId)
        {
            var alert = await _db.OfficeAlerts.FindAsync(alertId);
            if (alert == null) return;
            alert.IsRead = true;
            alert.ReadAt = DateTime.UtcNow;
            alert.ReadByUserId = userId;
            await _db.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            var unread = await _db.OfficeAlerts
                .Where(a => !a.IsRead)
                .ToListAsync();

            foreach (var a in unread)
            {
                a.IsRead = true;
                a.ReadAt = DateTime.UtcNow;
                a.ReadByUserId = userId;
            }
            await _db.SaveChangesAsync();
        }

        public async Task ResolveAlertAsync(Guid alertId)
        {
            var alert = await _db.OfficeAlerts.FindAsync(alertId);
            if (alert == null) return;
            alert.IsResolved = true;
            alert.ResolvedAt = DateTime.UtcNow;
            if (!alert.IsRead) alert.IsRead = true;
            await _db.SaveChangesAsync();
        }

        public async Task CreateAlertAsync(Guid officeId, AlertType type, AlertSeverity severity,
            string title, string message, string? referenceId = null, string? referenceType = null)
        {
            var alert = new OfficeAlert
            {
                Id = Guid.NewGuid(),
                OfficeId = officeId,
                AlertType = type,
                Severity = severity,
                Title = title,
                Message = message,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                CreatedDate = DateTime.UtcNow
            };
            _db.OfficeAlerts.Add(alert);
            await _db.SaveChangesAsync();
        }

        public async Task CheckLowBalancesAsync(decimal threshold = 100)
        {
            var lowBalances = await _db.VaultBalances
                .AsNoTracking()
                .Include(vb => vb.Vault).ThenInclude(v => v.Office)
                .Include(vb => vb.Currency)
                .Where(vb => vb.Balance > 0 && vb.Balance < threshold && vb.Vault.IsActive)
                .ToListAsync();

            foreach (var vb in lowBalances)
            {
                var exists = await _db.OfficeAlerts
                    .AnyAsync(a => a.OfficeId == vb.Vault.OfficeId &&
                                  a.AlertType == AlertType.LowBalance &&
                                  a.ReferenceId == vb.Id.ToString() &&
                                  !a.IsResolved);
                if (exists) continue;

                await CreateAlertAsync(
                    vb.Vault.OfficeId,
                    AlertType.LowBalance,
                    AlertSeverity.Warning,
                    $"Düşük bakiye: {vb.Currency.CurrencyCode}",
                    $"{vb.Vault.Name} kasasında {vb.Currency.CurrencyCode} bakiyesi {vb.Balance:N2} seviyesine düştü.",
                    vb.Id.ToString(),
                    "VaultBalance"
                );
            }
        }

        private static vm_alert MapToVm(OfficeAlert a) => new()
        {
            Id = a.Id,
            OfficeId = a.OfficeId,
            OfficeName = a.Office?.OfficeName ?? "",
            AlertType = a.AlertType.ToString(),
            Severity = a.Severity.ToString(),
            Title = a.Title,
            Message = a.Message,
            IsRead = a.IsRead,
            IsResolved = a.IsResolved,
            CreatedDate = a.CreatedDate,
            ReferenceId = a.ReferenceId,
            ReferenceType = a.ReferenceType
        };
    }
}
