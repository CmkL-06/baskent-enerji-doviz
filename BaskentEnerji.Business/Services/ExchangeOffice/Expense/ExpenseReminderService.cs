using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Expense;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Expense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Expense
{
    /// <summary>
    /// Tekrarlayan gider kalemlerinin vade tarihini hesaplar. TAMAMEN salt-okunur — hiçbir
    /// tabloya yazmaz, hiçbir zaman kalıcı bir "hatırlatma" durumu saklamaz; her çağrıda gerçek
    /// ExpensePayment geçmişinden anlık türetilir. Bu yüzden kasayı etkileyen hiçbir kod yoluna
    /// (CreatePaymentAsync/DeletePaymentAsync/ApproveExpensePaymentAsync) dokunmaz ve stale
    /// hatırlatma durumu riski kod seviyesinde yoktur.
    /// </summary>
    public class ExpenseReminderService : IExpenseReminderService
    {
        private readonly BaskentEnerjiDbContext _context;
        private static readonly TimeZoneInfo TurkeyTz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

        public ExpenseReminderService(BaskentEnerjiDbContext context)
        {
            _context = context;
        }

        public async Task<List<vm_expensedefinition>> GetUpcomingRecurringAsync(Guid officeId)
        {
            var definitions = await _context.ExpenseDefinitions
                .Include(ed => ed.Office)
                .Include(ed => ed.DefaultCurrency)
                .Include(ed => ed.Category)
                .Where(ed => ed.OfficeId == officeId && ed.IsActive && ed.IsRecurring)
                .ToListAsync();

            var result = new List<vm_expensedefinition>();

            foreach (var definition in definitions)
            {
                var (nextDueDate, dueStatus) = await CalculateNextDueInternalAsync(definition);
                if (dueStatus == null)
                    continue; // Henüz uzak — listelenmiyor

                result.Add(new vm_expensedefinition
                {
                    Id = definition.Id,
                    OfficeId = definition.OfficeId,
                    OfficeName = definition.Office?.OfficeName,
                    Code = definition.Code,
                    Name = definition.Name,
                    CategoryId = definition.CategoryId,
                    CategoryName = definition.Category?.Name,
                    Description = definition.Description,
                    IsActive = definition.IsActive,
                    IsRecurring = definition.IsRecurring,
                    RecurrencePeriod = definition.RecurrencePeriod,
                    RecurrencePeriodName = definition.RecurrencePeriod.HasValue ? GetRecurrencePeriodName(definition.RecurrencePeriod.Value) : null,
                    DefaultAmount = definition.DefaultAmount,
                    DefaultCurrencyId = definition.DefaultCurrencyId,
                    DefaultCurrencyCode = definition.DefaultCurrency?.CurrencyCode,
                    CreatedDate = definition.CreatedDate,
                    AccountReference = definition.AccountReference,
                    DueDayOfMonth = definition.DueDayOfMonth,
                    NextDueDate = nextDueDate,
                    DueStatus = dueStatus,
                });
            }

            return result.OrderBy(r => r.NextDueDate).ToList();
        }

        public async Task<(DateTime? nextDueDate, string? dueStatus)> CalculateNextDueAsync(Guid definitionId)
        {
            var definition = await _context.ExpenseDefinitions.FirstOrDefaultAsync(ed => ed.Id == definitionId);
            if (definition == null)
                return (null, null);

            return await CalculateNextDueInternalAsync(definition);
        }

        private async Task<(DateTime? nextDueDate, string? dueStatus)> CalculateNextDueInternalAsync(ExpenseDefinition definition)
        {
            if (!definition.IsRecurring || !definition.RecurrencePeriod.HasValue)
                return (null, null);

            // Silinmemiş SON ödeme (Paid VEYA Pending — Pending de "bu dönem için işlem
            // başlatıldı" sayılır, aksi halde onay bekleyen bir ödeme için de tekrar hatırlatma
            // gösterilir).
            var lastPayment = await _context.ExpensePayments
                .Where(ep => ep.ExpenseDefinitionId == definition.Id && !ep.IsDeleted
                    && (ep.Status == ExpenseStatus.Paid || ep.Status == ExpenseStatus.Pending))
                .OrderByDescending(ep => ep.PaymentDate)
                .FirstOrDefaultAsync();

            var baseDate = (lastPayment?.PaymentDate ?? definition.CreatedDate).Date;
            var period = definition.RecurrencePeriod.Value;

            DateTime nextDueDate;
            if (definition.DueDayOfMonth.HasValue && period == RecurrencePeriod.Monthly)
            {
                nextDueDate = NextCalendarDueDate(baseDate, definition.DueDayOfMonth.Value);
            }
            else
            {
                nextDueDate = AddPeriod(baseDate, period);
            }

            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTz).Date;

            // Sabit bir hatırlatma penceresi kullanılıyor (dönem uzunluğuna bağlı DEĞİL) — aksi
            // halde örn. aylık bir kalem ödendiği anda "sıradaki vade ~1 ay sonra" zaten dönem
            // uzunluğuna yakın olacağından her zaman "Yaklaşıyor" görünür, filtre anlamsızlaşırdı.
            string? status;
            if (nextDueDate < today)
                status = "Gecikmiş";
            else if (nextDueDate <= today.AddDays(7))
                status = "Bu Hafta";
            else if (nextDueDate <= today.AddDays(14))
                status = "Yaklaşıyor";
            else
                status = null; // Henüz uzak

            return (nextDueDate, status);
        }

        /// <summary>
        /// baseDate'ten SONRAKİ ilk takvim günü olan dueDay'i döndürür (bu ayın günü zaten
        /// geçtiyse bir sonraki aya kayar). Ay taşması durumunda (örn. dueDay=31 ama Şubat) o
        /// ayın son gününe sabitlenir.
        /// </summary>
        private static DateTime NextCalendarDueDate(DateTime baseDate, int dueDay)
        {
            DateTime ClampToMonth(int year, int month, int day)
            {
                var daysInMonth = DateTime.DaysInMonth(year, month);
                return new DateTime(year, month, Math.Min(day, daysInMonth));
            }

            var candidate = ClampToMonth(baseDate.Year, baseDate.Month, dueDay);
            if (candidate > baseDate)
                return candidate;

            var nextMonth = baseDate.AddMonths(1);
            return ClampToMonth(nextMonth.Year, nextMonth.Month, dueDay);
        }

        private static DateTime AddPeriod(DateTime date, RecurrencePeriod period)
        {
            return period switch
            {
                RecurrencePeriod.Daily => date.AddDays(1),
                RecurrencePeriod.Weekly => date.AddDays(7),
                RecurrencePeriod.Monthly => date.AddMonths(1),
                RecurrencePeriod.Quarterly => date.AddMonths(3),
                RecurrencePeriod.Yearly => date.AddYears(1),
                _ => date.AddMonths(1)
            };
        }

        private static string GetRecurrencePeriodName(RecurrencePeriod period)
        {
            return period switch
            {
                RecurrencePeriod.Daily => "Günlük",
                RecurrencePeriod.Weekly => "Haftalık",
                RecurrencePeriod.Monthly => "Aylık",
                RecurrencePeriod.Quarterly => "3 Aylık",
                RecurrencePeriod.Yearly => "Yıllık",
                _ => period.ToString()
            };
        }
    }
}
