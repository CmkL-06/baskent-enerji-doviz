using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Expense;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Expense;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.ExchangeOffice.Expense
{
    public interface IExpenseBudgetService
    {
        Task SetBudgetAsync(rm_expensebudget request);
        Task<List<vm_expensebudgetstatus>> GetBudgetStatusAsync(Guid officeId, int year, int month);
        Task<List<vm_expensebudgethistory>> GetBudgetHistoryAsync(Guid officeId, int months = 12);
    }
}
