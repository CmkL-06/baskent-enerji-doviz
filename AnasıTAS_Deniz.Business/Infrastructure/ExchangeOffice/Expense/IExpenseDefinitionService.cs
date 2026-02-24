using AnasıTAS_Deniz.Entity.Modals.RequestModals.ExchangeService.Expense;
using AnasıTAS_Deniz.Entity.Modals.ViewModals.ExchangeOFfice.Expense;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Infrastructure.ExchangeOffice.Expense
{
    public interface IExpenseDefinitionService
    {
        Task<vm_expensedefinition> CreateDefinitionAsync(rm_expensedefinition request);
        Task<vm_expensedefinition> UpdateDefinitionAsync(rm_expensedefinition request);
        Task<bool> DeleteDefinitionAsync(Guid id);
        Task<vm_expensedefinition> GetDefinitionAsync(Guid id);
        Task<List<vm_expensedefinition>> GetDefinitionsAsync(Guid officeId, bool? isActive = null);
        Task<List<vm_expensedefinition>> GetDefinitionsByCategoryAsync(Guid officeId, int category);
        Task<bool> IsCodeUniqueAsync(Guid officeId, string code, Guid? excludeId = null);
    }
}