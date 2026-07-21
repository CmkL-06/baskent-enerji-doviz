using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Expense;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.ExchangeOffice.Expense
{
    public interface IExpenseReminderService
    {
        // Salt-okunur — hiçbir tabloya yazmaz, her çağrıda gerçek ExpensePayment geçmişinden
        // anlık hesaplanır. Yalnızca Gecikmiş/Bu Hafta/Yaklaşıyor durumundaki tekrarlayan
        // kalemleri döndürür (henüz uzak olanlar listelenmez).
        Task<List<vm_expensedefinition>> GetUpcomingRecurringAsync(Guid officeId);

        // Tek bir tanım için vade hesabı — GetDefinitionStatementAsync tarafından da kullanılır,
        // aynı hesaplamanın iki yerde tekrarlanmaması için.
        Task<(DateTime? nextDueDate, string? dueStatus)> CalculateNextDueAsync(Guid definitionId);
    }
}
