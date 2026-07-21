using System;
using System.Linq;
using System.Threading.Tasks;
using BaskentEnerji.Business.Services.ExchangeOffice.Expense;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Expense;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BaskentEnerji.Business.Tests
{
    /// <summary>
    /// ExpenseCategoryService — Currency'deki upsert deseninin uyarlaması. Kritik olan tek fark:
    /// kullanımda olan bir kategori (ExpenseDefinition veya ExpenseBudget tarafından referans
    /// edilen) silinemez — aksi halde geçmiş kayıtlar öksüz kalır.
    /// </summary>
    [Collection("SharedTestDatabase")]
    public class ExpenseCategoryServiceTests
    {
        private ExpenseCategoryService CreateService(BaskentEnerjiDbContext ctx) => new ExpenseCategoryService(ctx);

        [Fact]
        public async Task SaveCategoryAsync_YeniKategori_OlusturulurVeListelenir()
        {
            var name = "Test Kategori " + Guid.NewGuid().ToString("N").Substring(0, 8);

            using (var ctx = TestDbContextFactory.Create())
            {
                var service = CreateService(ctx);
                var result = await service.SaveCategoryAsync(new rm_expensecategory { Name = name, IsActive = true });
                Assert.NotEqual(Guid.Empty, result.Id);
            }

            using var verifyCtx = TestDbContextFactory.Create();
            var exists = await verifyCtx.ExpenseCategories.AnyAsync(c => c.Name == name);
            Assert.True(exists);
        }

        [Fact]
        public async Task SaveCategoryAsync_MevcutId_YenidenAdlandirirVePasiflestirir()
        {
            Guid categoryId;
            using (var ctx = TestDbContextFactory.Create())
            {
                var category = new ExpenseCategoryDefinition { Id = Guid.NewGuid(), Name = "Eski Ad " + Guid.NewGuid().ToString("N").Substring(0, 8), IsActive = true };
                ctx.ExpenseCategories.Add(category);
                await ctx.SaveChangesAsync();
                categoryId = category.Id;
            }

            var newName = "Yeni Ad " + Guid.NewGuid().ToString("N").Substring(0, 8);
            using (var ctx = TestDbContextFactory.Create())
            {
                var service = CreateService(ctx);
                await service.SaveCategoryAsync(new rm_expensecategory { Id = categoryId, Name = newName, IsActive = false });
            }

            using var verifyCtx = TestDbContextFactory.Create();
            var updated = await verifyCtx.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == categoryId);
            Assert.NotNull(updated);
            Assert.Equal(newName, updated!.Name);
            Assert.False(updated.IsActive);
        }

        [Fact]
        public async Task SaveCategoryAsync_MukerrerAd_HataVerir()
        {
            var name = "Mükerrer " + Guid.NewGuid().ToString("N").Substring(0, 8);

            using (var ctx = TestDbContextFactory.Create())
            {
                var service = CreateService(ctx);
                await service.SaveCategoryAsync(new rm_expensecategory { Name = name, IsActive = true });
            }

            using var ctx2 = TestDbContextFactory.Create();
            var service2 = CreateService(ctx2);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service2.SaveCategoryAsync(new rm_expensecategory { Name = name, IsActive = true }));
        }

        [Fact]
        public async Task DeleteCategoryAsync_KullanilmayanKategori_Silinir()
        {
            Guid categoryId;
            using (var ctx = TestDbContextFactory.Create())
            {
                var category = new ExpenseCategoryDefinition { Id = Guid.NewGuid(), Name = "Silinecek " + Guid.NewGuid().ToString("N").Substring(0, 8), IsActive = true };
                ctx.ExpenseCategories.Add(category);
                await ctx.SaveChangesAsync();
                categoryId = category.Id;
            }

            using (var ctx = TestDbContextFactory.Create())
            {
                var service = CreateService(ctx);
                await service.DeleteCategoryAsync(categoryId);
            }

            using var verifyCtx = TestDbContextFactory.Create();
            var exists = await verifyCtx.ExpenseCategories.AnyAsync(c => c.Id == categoryId);
            Assert.False(exists);
        }

        [Fact]
        public async Task DeleteCategoryAsync_ExpenseDefinitionTarafindanKullaniliyorsa_HataVerirVeSilinmez()
        {
            Guid categoryId;
            using (var ctx = TestDbContextFactory.Create())
            {
                var office = new Office { Id = Guid.NewGuid(), OfficeName = "ExpenseCategory Test Ofis " + Guid.NewGuid() };
                var category = new ExpenseCategoryDefinition { Id = Guid.NewGuid(), Name = "Kullanımda " + Guid.NewGuid().ToString("N").Substring(0, 8), IsActive = true };
                var definition = new ExpenseDefinition
                {
                    Id = Guid.NewGuid(),
                    OfficeId = office.Id,
                    Code = "CAT-USE-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    Name = "Test Gider",
                    CategoryId = category.Id,
                    IsActive = true
                };

                ctx.Offices.Add(office);
                ctx.ExpenseCategories.Add(category);
                ctx.ExpenseDefinitions.Add(definition);
                await ctx.SaveChangesAsync();
                categoryId = category.Id;
            }

            using (var ctx = TestDbContextFactory.Create())
            {
                var service = CreateService(ctx);
                await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteCategoryAsync(categoryId));
            }

            using var verifyCtx = TestDbContextFactory.Create();
            var stillExists = await verifyCtx.ExpenseCategories.AnyAsync(c => c.Id == categoryId);
            Assert.True(stillExists);
        }

        [Fact]
        public async Task GetAllCategoriesAsync_UsageCountDoguHesaplanir()
        {
            Guid categoryId;
            using (var ctx = TestDbContextFactory.Create())
            {
                var office = new Office { Id = Guid.NewGuid(), OfficeName = "ExpenseCategory Usage Test Ofis " + Guid.NewGuid() };
                var category = new ExpenseCategoryDefinition { Id = Guid.NewGuid(), Name = "Kullanım Sayaç " + Guid.NewGuid().ToString("N").Substring(0, 8), IsActive = true };
                var definition = new ExpenseDefinition
                {
                    Id = Guid.NewGuid(),
                    OfficeId = office.Id,
                    Code = "CAT-COUNT-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    Name = "Test Gider",
                    CategoryId = category.Id,
                    IsActive = true
                };

                ctx.Offices.Add(office);
                ctx.ExpenseCategories.Add(category);
                ctx.ExpenseDefinitions.Add(definition);
                await ctx.SaveChangesAsync();
                categoryId = category.Id;
            }

            using var queryCtx = TestDbContextFactory.Create();
            var service = CreateService(queryCtx);
            var all = await service.GetAllCategoriesAsync();

            var found = all.FirstOrDefault(c => c.Id == categoryId);
            Assert.NotNull(found);
            Assert.Equal(1, found!.UsageCount);
        }
    }
}
