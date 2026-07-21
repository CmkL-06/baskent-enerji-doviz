using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Expense;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Expense;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.ExchangeOffice.Expense
{
    public interface IExpenseCategoryService
    {
        Task<List<vm_expensecategory>> GetAllCategoriesAsync();
        Task<vm_expensecategory> SaveCategoryAsync(rm_expensecategory request);
        Task DeleteCategoryAsync(Guid id);
    }
}
