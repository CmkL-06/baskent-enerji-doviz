using System;
using System.Threading.Tasks;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Services.ExchangeOffice.Expense;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Entities.User;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Expense;
using Xunit;

namespace BaskentEnerji.Business.Tests
{
    /// <summary>
    /// ExpenseDefinitionService'in yetkilendirme kontrolü (Viewer reddedilir) ve OfficeId+Code
    /// benzersizliği (uygulama katmanı ön-kontrolü + DB unique index'in yarış durumu koruması).
    /// </summary>
    [Collection("SharedTestDatabase")]
    public class ExpenseDefinitionServiceTests
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

            var office = new Office { Id = Guid.NewGuid(), OfficeName = "ExpenseDef Test Ofis " + Guid.NewGuid() };
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

        private ExpenseDefinitionService CreateService(BaskentEnerjiDbContext ctx, Guid actingUserId)
        {
            var validationService = TestAuthHelper.CreateValidationService(actingUserId, ctx);
            return new ExpenseDefinitionService(ctx, null!, validationService, new ExpenseReminderService(ctx));
        }

        private rm_expensedefinition BuildRequest(Guid officeId, string code, Guid categoryId, Guid? id = null) => new rm_expensedefinition
        {
            Id = id,
            OfficeId = officeId,
            Code = code,
            Name = "Test Gider Türü",
            CategoryId = categoryId,
            IsActive = true,
        };

        [Fact]
        public async Task CreateDefinitionAsync_ViewerRolu_ForbiddenAtar()
        {
            var s = await SeedAsync();
            using var ctx = TestDbContextFactory.Create();
            var service = CreateService(ctx, s.ViewerUserId);

            await Assert.ThrowsAsync<ApiException>(() => service.CreateDefinitionAsync(BuildRequest(s.OfficeId, "V1-" + Guid.NewGuid().ToString("N").Substring(0, 8), s.CategoryId)));
        }

        [Fact]
        public async Task UpdateDefinitionAsync_ViewerRolu_ForbiddenAtar()
        {
            var s = await SeedAsync();
            Guid defId;
            using (var ctx = TestDbContextFactory.Create())
            {
                var cashierService = CreateService(ctx, s.CashierUserId);
                var created = await cashierService.CreateDefinitionAsync(BuildRequest(s.OfficeId, "U1-" + Guid.NewGuid().ToString("N").Substring(0, 8), s.CategoryId));
                defId = created.Id;
            }

            using var viewerCtx = TestDbContextFactory.Create();
            var viewerService = CreateService(viewerCtx, s.ViewerUserId);
            await Assert.ThrowsAsync<ApiException>(() => viewerService.UpdateDefinitionAsync(BuildRequest(s.OfficeId, "U1-DEGISTI", s.CategoryId, defId)));
        }

        [Fact]
        public async Task DeleteDefinitionAsync_ViewerRolu_ForbiddenAtar()
        {
            var s = await SeedAsync();
            Guid defId;
            using (var ctx = TestDbContextFactory.Create())
            {
                var cashierService = CreateService(ctx, s.CashierUserId);
                var created = await cashierService.CreateDefinitionAsync(BuildRequest(s.OfficeId, "D1-" + Guid.NewGuid().ToString("N").Substring(0, 8), s.CategoryId));
                defId = created.Id;
            }

            using var viewerCtx = TestDbContextFactory.Create();
            var viewerService = CreateService(viewerCtx, s.ViewerUserId);
            await Assert.ThrowsAsync<ApiException>(() => viewerService.DeleteDefinitionAsync(defId));
        }

        [Fact]
        public async Task CreateDefinitionAsync_ViewerOlmayanRol_BasariliOlur()
        {
            var s = await SeedAsync();
            using var ctx = TestDbContextFactory.Create();
            var service = CreateService(ctx, s.CashierUserId);

            var result = await service.CreateDefinitionAsync(BuildRequest(s.OfficeId, "C1-" + Guid.NewGuid().ToString("N").Substring(0, 8), s.CategoryId));

            Assert.NotEqual(Guid.Empty, result.Id);
        }

        [Fact]
        public async Task CreateDefinitionAsync_AyniOfistekiMukerrerKod_HataVerir()
        {
            var s = await SeedAsync();
            var code = "DUP-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            using (var ctx = TestDbContextFactory.Create())
            {
                var service = CreateService(ctx, s.CashierUserId);
                await service.CreateDefinitionAsync(BuildRequest(s.OfficeId, code, s.CategoryId));
            }

            using var ctx2 = TestDbContextFactory.Create();
            var service2 = CreateService(ctx2, s.CashierUserId);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service2.CreateDefinitionAsync(BuildRequest(s.OfficeId, code, s.CategoryId)));
        }

        [Fact]
        public async Task CreateDefinitionAsync_AyniOfisAyniKodParalelIkiIstek_YalnizcaBiriBasarili()
        {
            var s = await SeedAsync();
            var code = "RACE-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            using var ctx1 = TestDbContextFactory.Create();
            using var ctx2 = TestDbContextFactory.Create();
            var service1 = CreateService(ctx1, s.CashierUserId);
            var service2 = CreateService(ctx2, s.CashierUserId);

            var task1 = service1.CreateDefinitionAsync(BuildRequest(s.OfficeId, code, s.CategoryId));
            var task2 = service2.CreateDefinitionAsync(BuildRequest(s.OfficeId, code, s.CategoryId));

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
