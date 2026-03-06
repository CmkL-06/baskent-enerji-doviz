using SmileMedical.Entity.Modals.RequestModals.ExchangeService.Expense;
using SmileMedical.Entity.Modals.ViewModals.ExchangeOFfice.Expense;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmileMedical.Business.Infrastructure.ExchangeOffice.Expense
{
    public interface IExpensePaymentService
    {
        Task<vm_expensepayment> CreatePaymentAsync(rm_expensepayment request);
        Task<vm_expensepayment> GetPaymentAsync(Guid id);
        Task<List<vm_expensepayment>> GetPaymentsAsync(Guid officeId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<vm_expensepayment>> GetPaymentsByDefinitionAsync(Guid definitionId, DateTime? startDate = null, DateTime? endDate = null);
        Task<bool> DeletePaymentAsync(Guid id, string reason);
        Task<decimal> GetTotalExpensesAsync(Guid officeId, DateTime startDate, DateTime endDate);
        Task<Dictionary<string, decimal>> GetExpensesByCategoryAsync(Guid officeId, DateTime startDate, DateTime endDate);
    }
}