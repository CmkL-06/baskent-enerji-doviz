using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Expense;
using BaskentEnerji.Business.Services.Permission;
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
    public class ExpenseBudgetService : IExpenseBudgetService
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly IExpensePaymentService _expensePaymentService;
        private readonly ValidationService _validationService;

        public ExpenseBudgetService(
            BaskentEnerjiDbContext context,
            IExpensePaymentService expensePaymentService,
            ValidationService validationService)
        {
            _context = context;
            _expensePaymentService = expensePaymentService;
            _validationService = validationService;
        }

        public async Task SetBudgetAsync(rm_expensebudget request)
        {
            await _validationService.EnsureNotViewerAsync(request.OfficeId);

            var officeExists = await _context.Offices.AnyAsync(o => o.Id == request.OfficeId);
            if (!officeExists)
                throw new InvalidOperationException("Belirtilen ofis bulunamadı. Lütfen sayfayı yenileyip tekrar deneyin.");

            var existing = await _context.ExpenseBudgets.FirstOrDefaultAsync(b =>
                b.OfficeId == request.OfficeId &&
                b.CategoryId == request.CategoryId &&
                b.Year == request.Year &&
                b.Month == request.Month);

            if (existing != null)
            {
                existing.BudgetAmount = request.BudgetAmount;
            }
            else
            {
                _context.ExpenseBudgets.Add(new ExpenseBudget
                {
                    Id = Guid.NewGuid(),
                    OfficeId = request.OfficeId,
                    CategoryId = request.CategoryId,
                    Year = request.Year,
                    Month = request.Month,
                    BudgetAmount = request.BudgetAmount,
                    CreatedByUserId = Guid.Parse(_validationService.GetUserID()),
                    CreatedDate = DateTime.UtcNow
                });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (
                ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx &&
                (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                // (OfficeId, Category, Year, Month) unique index'ine eşzamanlı bir çakışma —
                // kullanıcıya anlaşılır bir mesajla bildir, ham 500 yerine.
                throw new InvalidOperationException("Bu kategori/ay için bütçe eşzamanlı olarak güncellendi. Lütfen tekrar deneyin.");
            }
        }

        public async Task<List<vm_expensebudgetstatus>> GetBudgetStatusAsync(Guid officeId, int year, int month)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddSeconds(-1);

            var budgets = await _context.ExpenseBudgets
                .Where(b => b.OfficeId == officeId && b.Year == year && b.Month == month)
                .ToListAsync();

            var actualsByCategoryId = await _expensePaymentService.GetExpensesByCategoryAsync(officeId, monthStart, monthEnd);

            var categories = await _context.ExpenseCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            var result = new List<vm_expensebudgetstatus>();

            foreach (var category in categories)
            {
                var budget = budgets.FirstOrDefault(b => b.CategoryId == category.Id);
                var actual = actualsByCategoryId.TryGetValue(category.Id, out var a) ? a : 0m;

                if (budget == null && actual == 0m)
                    continue; // Ne bütçesi ne gerçekleşeni olan kategoriyi listede göstermeye gerek yok

                var budgetAmount = budget?.BudgetAmount ?? 0m;
                var percentUsed = budgetAmount > 0 ? (actual / budgetAmount) * 100m : 0m;
                var statusLevel = budget == null
                    ? "none"
                    : percentUsed >= 100m ? "red"
                    : percentUsed >= 80m ? "amber"
                    : "green";

                result.Add(new vm_expensebudgetstatus
                {
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    BudgetAmount = budgetAmount,
                    ActualAmount = actual,
                    PercentUsed = percentUsed,
                    StatusLevel = statusLevel
                });
            }

            return result.OrderByDescending(r => r.ActualAmount).ToList();
        }

        public async Task<List<vm_expensebudgethistory>> GetBudgetHistoryAsync(Guid officeId, int months = 12)
        {
            var result = new List<vm_expensebudgethistory>();
            var current = DateTime.UtcNow.Date;

            for (int i = months - 1; i >= 0; i--)
            {
                var monthDate = current.AddMonths(-i);
                var year = monthDate.Year;
                var month = monthDate.Month;
                var monthStart = new DateTime(year, month, 1);
                var monthEnd = monthStart.AddMonths(1).AddSeconds(-1);

                var totalBudget = await _context.ExpenseBudgets
                    .Where(b => b.OfficeId == officeId && b.Year == year && b.Month == month)
                    .SumAsync(b => (decimal?)b.BudgetAmount) ?? 0m;

                var totalActual = await _expensePaymentService.GetTotalExpensesAsync(officeId, monthStart, monthEnd);

                result.Add(new vm_expensebudgethistory
                {
                    Year = year,
                    Month = month,
                    Label = $"{MonthShortName(month)} {year}",
                    TotalBudget = totalBudget,
                    TotalActual = totalActual
                });
            }

            return result;
        }

        private static string MonthShortName(int month)
        {
            string[] names = { "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" };
            return names[month - 1];
        }

    }
}
