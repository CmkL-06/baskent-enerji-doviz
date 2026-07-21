using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Expense;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Expense;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.ExchangeOffice.Expense
{
    public interface IExpenseDefinitionService
    {
        Task<vm_expensedefinition> CreateDefinitionAsync(rm_expensedefinition request);
        Task<vm_expensedefinition> UpdateDefinitionAsync(rm_expensedefinition request);
        Task<bool> DeleteDefinitionAsync(Guid id);
        Task<vm_expensedefinition> GetDefinitionAsync(Guid id);
        Task<List<vm_expensedefinition>> GetDefinitionsAsync(Guid officeId, bool? isActive = null);
        Task<List<vm_expensedefinition>> GetDefinitionsByCategoryAsync(Guid officeId, Guid categoryId);
        Task<bool> IsCodeUniqueAsync(Guid officeId, string code, Guid? excludeId = null);
        Task<vm_expensedefinitionstatement> GetDefinitionStatementAsync(Guid definitionId, DateTime? fromDate = null, DateTime? toDate = null);
    }
}