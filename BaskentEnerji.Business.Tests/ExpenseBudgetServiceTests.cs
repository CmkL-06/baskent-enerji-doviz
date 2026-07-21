using System;
using System.Linq;
using System.Threading.Tasks;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Services.ExchangeOffice.Expense;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Entities.User;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Expense;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BaskentEnerji.Business.Tests
{
    /// <summary>
    /// ExpenseBudgetService.SetBudgetAsync'in yetkilendirme kontrolü, upsert mantığı ve
    /// (OfficeId, Category, Year, Month) unique index'ine dayanan eşzamanlılık koruması.
    /// </summary>
    [Collection("SharedTestDatabase")]
    public class ExpenseBudgetServiceTests
    {
        private class TestScenario
        {
            public Guid OfficeId;
            public Guid ViewerUserId;
            public Guid CashierUserId;
            public Guid CategoryId;
        }

        private async Task<TestScenario> SeedAsync()
        {
            using var ctx = TestDbContextFactory.Create();

            var office = new Office { Id = Guid.NewGuid(), OfficeName = "ExpenseBudget Test Ofis " + Guid.NewGuid() };
            var viewer = new User { Id = Guid.NewGuid(), Username = "viewer_" + Guid.NewGuid(), Password = "", Mail = "", Firstname = "Test", Lastname = "Viewer", Rank = Rank.Staff };
            var cashier = new User { Id = Guid.NewGuid(), Username = "cashier_" + Guid.NewGuid(), Password = "", Mail = "", Firstname = "Test", Lastname = "Cashier", Rank = Rank.Staff };
            var category = new ExpenseCategoryDefinition { Id = Guid.NewGuid(), Name = "Test Kategori " + Guid.NewGuid().ToString("N").Substring(0, 8), IsActive = true };

            ctx.Offices.Add(office);
            ctx.Users.AddRange(viewer, cashier);
            ctx.ExpenseCategories.Add(category);
            ctx.User_Offices.Add(new User_Office { Id = Guid.NewGuid(), UserId = viewer.Id, OfficeId = office.Id, Role = OfficeRole.Viewer, IsActive = true });
            ctx.User_Offices.Add(new User_Office { Id = Guid.NewGuid(), UserId = cashier.Id, OfficeId = office.Id, Role = OfficeRole.Cashier, IsActive = true });
            await ctx.SaveChangesAsync();

            return new TestScenario { OfficeId = office.Id, ViewerUserId = viewer.Id, CashierUserId = cashier.Id, CategoryId = category.Id };
        }

        private ExpenseBudgetService CreateService(BaskentEnerjiDbContext ctx, Guid actingUserId)
        {
            var validationService = TestAuthHelper.CreateValidationService(actingUserId, ctx);
            return new ExpenseBudgetService(ctx, null!, validationService);
        }

        [Fact]
        public async Task SetBudgetAsync_ViewerRolu_ForbiddenAtar()
        {
            var s = await SeedAsync();
            using var ctx = TestDbContextFactory.Create();
            var service = CreateService(ctx, s.ViewerUserId);

            await Assert.ThrowsAsync<ApiException>(() => service.SetBudgetAsync(new rm_expensebudget
            {
                OfficeId = s.OfficeId,
                CategoryId = s.CategoryId,
                Year = 2026,
                Month = 1,
                BudgetAmount = 10000m
            }));
        }

        [Fact]
        public async Task SetBudgetAsync_YeniKayit_OlusturulurVeVarOlanGuncellenir()
        {
            var s = await SeedAsync();
            using (var ctx = TestDbContextFactory.Create())
            {
                var service = CreateService(ctx, s.CashierUserId);
                await service.SetBudgetAsync(new rm_expensebudget
                {
                    OfficeId = s.OfficeId,
                    CategoryId = s.CategoryId,
                    Year = 2026,
                    Month = 3,
                    BudgetAmount = 5000m
                });
            }

            using (var verifyCtx = TestDbContextFactory.Create())
            {
                var budget = await verifyCtx.ExpenseBudgets.FirstOrDefaultAsync(b =>
                    b.OfficeId == s.OfficeId && b.CategoryId == s.CategoryId && b.Year == 2026 && b.Month == 3);
                Assert.NotNull(budget);
                Assert.Equal(5000m, budget!.BudgetAmount);
            }

            using (var ctx = TestDbContextFactory.Create())
            {
                var service = CreateService(ctx, s.CashierUserId);
                await service.SetBudgetAsync(new rm_expensebudget
                {
                    OfficeId = s.OfficeId,
                    CategoryId = s.CategoryId,
                    Year = 2026,
                    Month = 3,
                    BudgetAmount = 7500m
                });
            }

            using (var verifyCtx = TestDbContextFactory.Create())
            {
                var budgets = verifyCtx.ExpenseBudgets.Where(b =>
                    b.OfficeId == s.OfficeId && b.CategoryId == s.CategoryId && b.Year == 2026 && b.Month == 3).ToList();
                Assert.Single(budgets);
                Assert.Equal(7500m, budgets[0].BudgetAmount);
            }
        }

        [Fact]
        public async Task SetBudgetAsync_AyniOfisKategoriAyParalelIkiIstek_BiriBasariliDigeriDostaneHata()
        {
            var s = await SeedAsync();

            using var ctx1 = TestDbContextFactory.Create();
            using var ctx2 = TestDbContextFactory.Create();
            var service1 = CreateService(ctx1, s.CashierUserId);
            var service2 = CreateService(ctx2, s.CashierUserId);

            var request1 = new rm_expensebudget { OfficeId = s.OfficeId, CategoryId = s.CategoryId, Year = 2026, Month = 6, BudgetAmount = 1000m };
            var request2 = new rm_expensebudget { OfficeId = s.OfficeId, CategoryId = s.CategoryId, Year = 2026, Month = 6, BudgetAmount = 2000m };

            var task1 = service1.SetBudgetAsync(request1);
            var task2 = service2.SetBudgetAsync(request2);

            await Task.WhenAll(task1.ContinueWith(t => t), task2.ContinueWith(t => t));

            int successCount = 0;
            int failureCount = 0;
            if (task1.IsCompletedSuccessfully) successCount++; else if (task1.Exception?.InnerException is InvalidOperationException) failureCount++;
            if (task2.IsCompletedSuccessfully) successCount++; else if (task2.Exception?.InnerException is InvalidOperationException) failureCount++;

            Assert.Equal(1, successCount);
            Assert.Equal(1, failureCount);
        }
    }
}
