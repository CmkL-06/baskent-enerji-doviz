using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Expense;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Expense;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Expense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Expense
{
    // Currency'deki upsert deseninin (ExchangeServiceCommand.SaveCurrency/DeleteCurrency) birebir
    // uyarlaması — TEK fark: silme, kullanımda olan bir kategoriyi engeller (Currency'de bu koruma
    // yok, ama bir gider kategorisi silinirse geçmiş ExpenseDefinition/ExpenseBudget kayıtları
    // öksüz kalır — bu "muhasebenin kalbi" ilkesiyle kabul edilemez).
    public class ExpenseCategoryService : IExpenseCategoryService
    {
        private readonly BaskentEnerjiDbContext _context;

        public ExpenseCategoryService(BaskentEnerjiDbContext context)
        {
            _context = context;
        }

        public async Task<List<vm_expensecategory>> GetAllCategoriesAsync()
        {
            var categories = await _context.ExpenseCategories
                .OrderBy(c => c.Name)
                .ToListAsync();

            var result = new List<vm_expensecategory>();
            foreach (var category in categories)
            {
                var usageCount = await _context.ExpenseDefinitions.CountAsync(ed => ed.CategoryId == category.Id)
                    + await _context.ExpenseBudgets.CountAsync(eb => eb.CategoryId == category.Id);

                result.Add(new vm_expensecategory
                {
                    Id = category.Id,
                    Name = category.Name,
                    IsActive = category.IsActive,
                    UsageCount = usageCount
                });
            }

            return result;
        }

        public async Task<vm_expensecategory> SaveCategoryAsync(rm_expensecategory request)
        {
            ExpenseCategoryDefinition category;

            if (request.Id.HasValue)
            {
                category = await _context.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == request.Id.Value);
                if (category == null)
                    throw new InvalidOperationException("Kategori bulunamadı.");

                category.Name = request.Name;
                category.IsActive = request.IsActive;
            }
            else
            {
                category = new ExpenseCategoryDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    IsActive = request.IsActive,
                    CreatedDate = DateTime.UtcNow
                };
                _context.ExpenseCategories.Add(category);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (
                ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx &&
                (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                throw new InvalidOperationException($"'{request.Name}' adında bir kategori zaten mevcut.");
            }

            return new vm_expensecategory
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive,
                UsageCount = 0
            };
        }

        public async Task DeleteCategoryAsync(Guid id)
        {
            var category = await _context.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
                return;

            var isInUse = await _context.ExpenseDefinitions.AnyAsync(ed => ed.CategoryId == id)
                || await _context.ExpenseBudgets.AnyAsync(eb => eb.CategoryId == id);

            if (isInUse)
                throw new InvalidOperationException("Bu kategori mevcut gider tanımları veya bütçelerde kullanıldığı için silinemez. Bunun yerine pasifleştirebilirsiniz.");

            _context.ExpenseCategories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}
