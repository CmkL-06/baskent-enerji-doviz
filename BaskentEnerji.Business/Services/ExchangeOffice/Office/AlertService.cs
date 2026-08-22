using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Infrastructure.Telegram;
using BaskentEnerji.Business.Services.User;
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
        private readonly ITelegramNotificationService _telegramNotificationService;

        public AlertService(BaskentEnerjiDbContext db, ITelegramNotificationService telegramNotificationService)
        {
            _db = db;
            _telegramNotificationService = telegramNotificationService;
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

            // Owner'lara Telegram bildirimi — sadece Warning/Critical (Info seviyesi paneldeki
            // listede kalır, bildirim spam'i yaratmasın diye Telegram'a gitmez).
            if (severity != AlertSeverity.Info)
                await NotifyOwnersViaTelegramAsync(alert);
        }

        private async Task NotifyOwnersViaTelegramAsync(OfficeAlert alert)
        {
            var ownerChatIds = await _db.Users
                .Where(u => u.Rank == Rank.Owner && u.TelegramOperatorId.HasValue)
                .Select(u => u.TelegramOperatorId!.Value)
                .ToListAsync();

            if (ownerChatIds.Count == 0) return;

            var severityLabel = alert.Severity == AlertSeverity.Critical ? "🚨 KRİTİK" : "⚠️ Uyarı";
            var text = $"{severityLabel}: {alert.Title}\n\n{alert.Message}";

            foreach (var chatId in ownerChatIds)
                await _telegramNotificationService.SendMessageAsync(chatId, text);
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

        public async Task CheckStaffDailyAnomaliesAsync(DateTime businessDate)
        {
            var dayStartUtc = businessDate.Date.AddHours(-3);
            var dayEndUtc = dayStartUtc.AddDays(1);
            var dateKey = businessDate.Date.ToString("yyyy-MM-dd");

            // 1. Gün sonu eksik: o gün işlemi olan ama kapanışı olmayan ofisler
            var officesWithTransactions = await _db.Transactions
                .Where(t => t.CreatedDate >= dayStartUtc && t.CreatedDate < dayEndUtc)
                .Select(t => t.Vault.OfficeId)
                .Distinct()
                .ToListAsync();

            foreach (var officeId in officesWithTransactions)
            {
                var hasClosure = await _db.DayClosures.AnyAsync(dc =>
                    dc.OfficeId == officeId &&
                    dc.BusinessDate.Date == businessDate.Date &&
                    (dc.Status == DayClosureStatus.Closed || dc.Status == DayClosureStatus.AutoClosed));
                if (hasClosure) continue;

                var closureReferenceId = $"{officeId}:{dateKey}";
                var closureAlertExists = await _db.OfficeAlerts.AnyAsync(a =>
                    a.AlertType == AlertType.DayClosureMissing &&
                    a.ReferenceId == closureReferenceId &&
                    !a.IsResolved);
                if (closureAlertExists) continue;

                var office = await _db.Offices.FindAsync(officeId);
                await CreateAlertAsync(
                    officeId,
                    AlertType.DayClosureMissing,
                    AlertSeverity.Warning,
                    "Gün sonu alınmadı",
                    $"{office?.OfficeName ?? "Ofis"} için {businessDate:dd.MM.yyyy} tarihinde işlem yapıldı ancak gün sonu kapanışı alınmadı.",
                    closureReferenceId,
                    "DayClosureMissing"
                );
            }

            // 2. Toplu/şüpheli giriş: personel + ofis bazında
            var staffTransactions = await _db.Transactions
                .Where(t => t.CreatedDate >= dayStartUtc && t.CreatedDate < dayEndUtc)
                .Select(t => new { t.UserId, t.CreatedDate, t.TransactionDate, OfficeId = t.Vault.OfficeId })
                .ToListAsync();

            var historyStartUtc = dayStartUtc.AddDays(-14);

            foreach (var group in staffTransactions.GroupBy(t => new { t.UserId, t.OfficeId }))
            {
                // Kişisel baseline: bu personelin son 14 günündeki (bugün hariç), en az 5 işlemli günlerinin dağılım oranları
                var historicalTransactions = await _db.Transactions
                    .Where(t => t.UserId == group.Key.UserId && t.CreatedDate >= historyStartUtc && t.CreatedDate < dayStartUtc)
                    .Select(t => t.CreatedDate)
                    .ToListAsync();

                var historicalDailyRatios = historicalTransactions
                    .GroupBy(d => d.AddHours(3).Date)
                    .Where(g => g.Count() >= 5)
                    .Select(g => BulkEntryHeuristic.ComputeSpreadRatio(g.ToList()).Value)
                    .ToList();

                var heuristic = BulkEntryHeuristic.Evaluate(
                    group.Select(t => (t.CreatedDate, t.TransactionDate)).ToList(),
                    historicalDailyRatios);
                if (!heuristic.IsBulkEntrySuspected) continue;

                var bulkReferenceId = $"{group.Key.UserId}:{dateKey}";
                var bulkAlertExists = await _db.OfficeAlerts.AnyAsync(a =>
                    a.AlertType == AlertType.BulkEntrySuspected &&
                    a.ReferenceId == bulkReferenceId &&
                    !a.IsResolved);
                if (bulkAlertExists) continue;

                var user = await _db.Users.FindAsync(group.Key.UserId);
                var userName = user != null ? $"{user.Firstname} {user.Lastname}".Trim() : "Personel";

                await CreateAlertAsync(
                    group.Key.OfficeId,
                    AlertType.BulkEntrySuspected,
                    AlertSeverity.Warning,
                    $"Toplu giriş şüphesi: {userName}",
                    $"{userName}, {businessDate:dd.MM.yyyy} tarihinde {heuristic.TransactionCount} işlemi {heuristic.FirstTransactionAt:HH:mm}–{heuristic.LastTransactionAt:HH:mm} arasında dar bir zaman diliminde girmiş.",
                    bulkReferenceId,
                    "BulkEntrySuspected"
                );
            }
        }

        // ShouldCount=true olan aktif kasalarda LastCountDate belirtilen gün sayısından eskiyse
        // (veya hiç sayılmadıysa) tekil bir Uyarı üretir. Kasa başına en fazla bir aktif uyarı olur —
        // sayım yapılınca ilgili uyarıyı çözer.
        public async Task CheckVaultCountOverdueAsync(int overdueDays = 14)
        {
            var thresholdDate = DateTime.Now.AddDays(-overdueDays);

            var vaults = await _db.Vaults
                .AsNoTracking()
                .Include(v => v.Office)
                .Where(v => v.IsActive && v.ShouldCount &&
                            (v.LastCountDate == null || v.LastCountDate < thresholdDate))
                .ToListAsync();

            foreach (var vault in vaults)
            {
                var referenceId = vault.Id.ToString();
                var exists = await _db.OfficeAlerts.AnyAsync(a =>
                    a.AlertType == AlertType.VaultCountOverdue &&
                    a.ReferenceId == referenceId &&
                    !a.IsResolved);
                if (exists) continue;

                var daysAgo = vault.LastCountDate.HasValue
                    ? (int)(DateTime.Now - vault.LastCountDate.Value).TotalDays
                    : (int?)null;

                var msg = daysAgo.HasValue
                    ? $"{vault.Name} kasası {daysAgo.Value} gündür sayılmadı (son sayım: {vault.LastCountDate:dd.MM.yyyy})."
                    : $"{vault.Name} kasası hiç sayılmadı.";

                await CreateAlertAsync(
                    vault.OfficeId,
                    AlertType.VaultCountOverdue,
                    AlertSeverity.Warning,
                    $"Kasa sayımı gecikti: {vault.Name}",
                    msg,
                    referenceId,
                    "Vault");
            }

            // Bu arada sayılmış olan kasaların açık uyarısını otomatik çöz — kullanıcı manuel
            // resolve etmek zorunda kalmasın.
            var openAlerts = await _db.OfficeAlerts
                .Where(a => a.AlertType == AlertType.VaultCountOverdue && !a.IsResolved)
                .ToListAsync();

            if (openAlerts.Count > 0)
            {
                var vaultLookup = await _db.Vaults
                    .AsNoTracking()
                    .Where(v => openAlerts.Select(a => a.ReferenceId).Contains(v.Id.ToString()))
                    .ToDictionaryAsync(v => v.Id.ToString(), v => v);

                foreach (var alert in openAlerts)
                {
                    if (alert.ReferenceId == null) continue;
                    if (!vaultLookup.TryGetValue(alert.ReferenceId, out var v)) continue;
                    // Kasa artık gecikmiş sayılmıyorsa çöz
                    if (v.LastCountDate.HasValue && v.LastCountDate.Value >= thresholdDate)
                    {
                        alert.IsResolved = true;
                        alert.ResolvedAt = DateTime.UtcNow;
                        if (!alert.IsRead) { alert.IsRead = true; alert.ReadAt = DateTime.UtcNow; }
                    }
                }
                await _db.SaveChangesAsync();
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
