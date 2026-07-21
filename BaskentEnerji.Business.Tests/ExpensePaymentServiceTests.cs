using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Services.ExchangeOffice.Expense;
using BaskentEnerji.Business.Services.ExchangeOffice.Office;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Entities.User;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Expense;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace BaskentEnerji.Business.Tests
{
    /// <summary>
    /// ExpensePaymentService'in onay eşiği (10.000 TL) mantığı: eşik altı her rol için hemen
    /// öder/kasa düşer; eşik üstü non-admin roller Pending oluşturur ve kasaya hiç dokunulmaz;
    /// Owner onayı kasayı tam bir kez düşürür (ve bakiyeyi tekrar kontrol eder); red kasaya hiç
    /// dokunmaz. Gerçek VaultService/WacService kullanılıyor (bkz. ExchangeTransactionServiceTests'teki
    /// aynı gerekçe — raw SQL UPDLOCK InMemory provider'da desteklenmiyor).
    /// </summary>
    [Collection("SharedTestDatabase")]
    public class ExpensePaymentServiceTests
    {
        private class TestScenario
        {
            public Guid OfficeId;
            public Guid VaultId;
            public Guid TryCurrencyId;
            public Guid ExpenseDefinitionId;
            public Guid OwnerUserId;
            public Guid StaffUserId;
        }

        private async Task<TestScenario> SeedAsync(decimal initialTryBalance = 1_000_000m)
        {
            using var ctx = TestDbContextFactory.Create();

            var office = new Office { Id = Guid.NewGuid(), OfficeName = "Expense Test Ofis " + Guid.NewGuid() };
            var vault = new Vault { Id = Guid.NewGuid(), Name = "Test Kasa", Description = "", OfficeId = office.Id, Type = Vault.VaultType.Main };

            var tryCurrency = await ctx.Currencies.FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
            if (tryCurrency == null)
            {
                tryCurrency = new Currency { Id = Guid.NewGuid(), CurrencyCode = "TRY" };
                ctx.Currencies.Add(tryCurrency);
            }

            var owner = new User { Id = Guid.NewGuid(), Username = "owner_" + Guid.NewGuid(), Password = "", Mail = "", Firstname = "Test", Lastname = "Owner", Rank = Rank.Owner };
            var staff = new User { Id = Guid.NewGuid(), Username = "staff_" + Guid.NewGuid(), Password = "", Mail = "", Firstname = "Test", Lastname = "Staff", Rank = Rank.Staff };

            var category = new ExpenseCategoryDefinition { Id = Guid.NewGuid(), Name = "Test Kategori " + Guid.NewGuid().ToString("N").Substring(0, 8), IsActive = true };

            var definition = new ExpenseDefinition
            {
                Id = Guid.NewGuid(),
                OfficeId = office.Id,
                Code = "TESTDEF",
                Name = "Test Gider Tanımı",
                CategoryId = category.Id,
                IsActive = true
            };

            ctx.Offices.Add(office);
            ctx.Vaults.Add(vault);
            ctx.Users.AddRange(owner, staff);
            ctx.ExpenseCategories.Add(category);
            ctx.ExpenseDefinitions.Add(definition);
            ctx.User_Offices.Add(new User_Office { Id = Guid.NewGuid(), UserId = staff.Id, OfficeId = office.Id, Role = OfficeRole.Cashier, IsActive = true });
            ctx.VaultBalances.Add(new VaultBalance { Id = Guid.NewGuid(), VaultId = vault.Id, CurrencyId = tryCurrency.Id, Balance = initialTryBalance });
            await ctx.SaveChangesAsync();

            return new TestScenario
            {
                OfficeId = office.Id,
                VaultId = vault.Id,
                TryCurrencyId = tryCurrency.Id,
                ExpenseDefinitionId = definition.Id,
                OwnerUserId = owner.Id,
                StaffUserId = staff.Id
            };
        }

        private ExpensePaymentService CreateService(BaskentEnerjiDbContext ctx, Guid actingUserId)
        {
            var validationService = TestAuthHelper.CreateValidationService(actingUserId, ctx);
            var vaultService = new VaultService(ctx, null!, new MemoryCache(new MemoryCacheOptions()), validationService);
            return new ExpensePaymentService(ctx, null!, vaultService, validationService);
        }

        private async Task<decimal> GetVaultBalanceAsync(Guid vaultId, Guid currencyId)
        {
            using var ctx = TestDbContextFactory.Create();
            var balance = await ctx.VaultBalances.FirstOrDefaultAsync(b => b.VaultId == vaultId && b.CurrencyId == currencyId);
            return balance?.Balance ?? 0m;
        }

        [Fact]
        public async Task CreatePaymentAsync_EsikAltiTutar_HemenOdenirVeKasaDuser()
        {
            var s = await SeedAsync();
            using var ctx = TestDbContextFactory.Create();
            var service = CreateService(ctx, s.StaffUserId);

            var result = await service.CreatePaymentAsync(new rm_expensepayment
            {
                ExpenseDefinitionId = s.ExpenseDefinitionId,
                CurrencyId = s.TryCurrencyId,
                PaymentDate = DateTime.UtcNow,
                Amount = 5000m, // Eşiğin (10.000) altında
                PaymentMethod = PaymentMethod.Cash
            });

            Assert.Equal(ExpenseStatus.Paid, result.Status);
            var balance = await GetVaultBalanceAsync(s.VaultId, s.TryCurrencyId);
            Assert.Equal(995_000m, balance); // 1.000.000 - 5.000
        }

        [Fact]
        public async Task CreatePaymentAsync_EsikUstuNonAdminOdeme_PendingOlurVeKasayaDokunulmaz()
        {
            var s = await SeedAsync();
            using var ctx = TestDbContextFactory.Create();
            var service = CreateService(ctx, s.StaffUserId);

            var result = await service.CreatePaymentAsync(new rm_expensepayment
            {
                ExpenseDefinitionId = s.ExpenseDefinitionId,
                CurrencyId = s.TryCurrencyId,
                PaymentDate = DateTime.UtcNow,
                Amount = 15000m, // Eşiğin (10.000) üzerinde
                PaymentMethod = PaymentMethod.Cash
            });

            Assert.Equal(ExpenseStatus.Pending, result.Status);
            var balance = await GetVaultBalanceAsync(s.VaultId, s.TryCurrencyId);
            Assert.Equal(1_000_000m, balance); // Hiç değişmemiş olmalı
        }

        [Fact]
        public async Task CreatePaymentAsync_OwnerEsikUstuOlsaDahi_HemenOdenir()
        {
            var s = await SeedAsync();
            using var ctx = TestDbContextFactory.Create();
            var service = CreateService(ctx, s.OwnerUserId);

            var result = await service.CreatePaymentAsync(new rm_expensepayment
            {
                ExpenseDefinitionId = s.ExpenseDefinitionId,
                CurrencyId = s.TryCurrencyId,
                PaymentDate = DateTime.UtcNow,
                Amount = 50000m,
                PaymentMethod = PaymentMethod.BankTransfer
            });

            Assert.Equal(ExpenseStatus.Paid, result.Status);
            var balance = await GetVaultBalanceAsync(s.VaultId, s.TryCurrencyId);
            Assert.Equal(950_000m, balance);
        }

        [Fact]
        public async Task ApproveExpensePaymentAsync_Onaylanirsa_KasaTamBirKezDuserVeBakiyeTekrarKontrolEdilir()
        {
            var s = await SeedAsync();
            Guid paymentId;
            using (var ctx = TestDbContextFactory.Create())
            {
                var staffService = CreateService(ctx, s.StaffUserId);
                var pending = await staffService.CreatePaymentAsync(new rm_expensepayment
                {
                    ExpenseDefinitionId = s.ExpenseDefinitionId,
                    CurrencyId = s.TryCurrencyId,
                    PaymentDate = DateTime.UtcNow,
                    Amount = 20000m,
                    PaymentMethod = PaymentMethod.Cash
                });
                paymentId = pending.Id;
            }

            // Onaydan önce kasa değişmemiş olmalı
            Assert.Equal(1_000_000m, await GetVaultBalanceAsync(s.VaultId, s.TryCurrencyId));

            using (var ownerCtx = TestDbContextFactory.Create())
            {
                var ownerService = CreateService(ownerCtx, s.OwnerUserId);
                var approved = await ownerService.ApproveExpensePaymentAsync(paymentId, true, null!);
                Assert.Equal(ExpenseStatus.Paid, approved.Status);
                Assert.Equal(s.OwnerUserId, ownerCtx.ExpensePayments.Find(paymentId)!.ApprovedByUserId);
            }

            // Onaydan sonra kasa TAM BİR KEZ düşmüş olmalı (double-deduction yok)
            Assert.Equal(980_000m, await GetVaultBalanceAsync(s.VaultId, s.TryCurrencyId));
        }

        [Fact]
        public async Task ApproveExpensePaymentAsync_Reddedilirse_KasayaDokunulmazVeCancelledOlur()
        {
            var s = await SeedAsync();
            Guid paymentId;
            using (var ctx = TestDbContextFactory.Create())
            {
                var staffService = CreateService(ctx, s.StaffUserId);
                var pending = await staffService.CreatePaymentAsync(new rm_expensepayment
                {
                    ExpenseDefinitionId = s.ExpenseDefinitionId,
                    CurrencyId = s.TryCurrencyId,
                    PaymentDate = DateTime.UtcNow,
                    Amount = 20000m,
                    PaymentMethod = PaymentMethod.Cash
                });
                paymentId = pending.Id;
            }

            using (var ownerCtx = TestDbContextFactory.Create())
            {
                var ownerService = CreateService(ownerCtx, s.OwnerUserId);
                var rejected = await ownerService.ApproveExpensePaymentAsync(paymentId, false, "Bütçe dışı harcama");
                Assert.Equal(ExpenseStatus.Cancelled, rejected.Status);
                Assert.Equal("Bütçe dışı harcama", rejected.RejectionNote);
            }

            Assert.Equal(1_000_000m, await GetVaultBalanceAsync(s.VaultId, s.TryCurrencyId));
        }

        [Fact]
        public async Task ApproveExpensePaymentAsync_ZatenSonuclanmisOdemeyiTekrarOnaylama_HataVerir()
        {
            var s = await SeedAsync();
            Guid paymentId;
            using (var ctx = TestDbContextFactory.Create())
            {
                var staffService = CreateService(ctx, s.StaffUserId);
                var pending = await staffService.CreatePaymentAsync(new rm_expensepayment
                {
                    ExpenseDefinitionId = s.ExpenseDefinitionId,
                    CurrencyId = s.TryCurrencyId,
                    PaymentDate = DateTime.UtcNow,
                    Amount = 20000m,
                    PaymentMethod = PaymentMethod.Cash
                });
                paymentId = pending.Id;
            }

            using (var ownerCtx = TestDbContextFactory.Create())
            {
                var ownerService = CreateService(ownerCtx, s.OwnerUserId);
                await ownerService.ApproveExpensePaymentAsync(paymentId, true, null!);
            }

            using var retryCtx = TestDbContextFactory.Create();
            var retryService = CreateService(retryCtx, s.OwnerUserId);
            await Assert.ThrowsAsync<ApiException>(() => retryService.ApproveExpensePaymentAsync(paymentId, true, null!));

            // İkinci onay denemesi kasaya tekrar dokunmamış olmalı
            Assert.Equal(980_000m, await GetVaultBalanceAsync(s.VaultId, s.TryCurrencyId));
        }

        [Fact]
        public async Task ApproveExpensePaymentAsync_OwnerOlmayan_ForbiddenAtar()
        {
            var s = await SeedAsync();
            Guid paymentId;
            using (var ctx = TestDbContextFactory.Create())
            {
                var staffService = CreateService(ctx, s.StaffUserId);
                var pending = await staffService.CreatePaymentAsync(new rm_expensepayment
                {
                    ExpenseDefinitionId = s.ExpenseDefinitionId,
                    CurrencyId = s.TryCurrencyId,
                    PaymentDate = DateTime.UtcNow,
                    Amount = 20000m,
                    PaymentMethod = PaymentMethod.Cash
                });
                paymentId = pending.Id;
            }

            using var staffCtx = TestDbContextFactory.Create();
            var staffService2 = CreateService(staffCtx, s.StaffUserId); // Owner değil

            await Assert.ThrowsAsync<ApiException>(() => staffService2.ApproveExpensePaymentAsync(paymentId, true, null!));
        }

        [Fact]
        public async Task DeletePaymentAsync_PendingOdemeSilinirse_KasayaDokunulmaz()
        {
            var s = await SeedAsync();
            Guid paymentId;
            using (var ctx = TestDbContextFactory.Create())
            {
                var staffService = CreateService(ctx, s.StaffUserId);
                var pending = await staffService.CreatePaymentAsync(new rm_expensepayment
                {
                    ExpenseDefinitionId = s.ExpenseDefinitionId,
                    CurrencyId = s.TryCurrencyId,
                    PaymentDate = DateTime.UtcNow,
                    Amount = 20000m, // Eşik üstü -> Pending, kasa hiç düşmez
                    PaymentMethod = PaymentMethod.Cash
                });
                paymentId = pending.Id;
            }

            using (var ctx = TestDbContextFactory.Create())
            {
                var ownerService = CreateService(ctx, s.OwnerUserId);
                var deleted = await ownerService.DeletePaymentAsync(paymentId, "Yanlışlıkla eklendi");
                Assert.True(deleted);
            }

            // Hayalet kasa yatırımı olmamalı — bakiye başlangıçtaki gibi kalmalı
            Assert.Equal(1_000_000m, await GetVaultBalanceAsync(s.VaultId, s.TryCurrencyId));

            using var verifyCtx = TestDbContextFactory.Create();
            var payment = verifyCtx.ExpensePayments.Find(paymentId);
            Assert.True(payment!.IsDeleted);
            Assert.Equal(ExpenseStatus.Cancelled, payment.Status);
        }

        [Fact]
        public async Task DeletePaymentAsync_PaidOdemeSilinirse_KasaTamOlarakEskiHalineDoner()
        {
            var s = await SeedAsync();
            Guid paymentId;
            using (var ctx = TestDbContextFactory.Create())
            {
                var staffService = CreateService(ctx, s.StaffUserId);
                var paid = await staffService.CreatePaymentAsync(new rm_expensepayment
                {
                    ExpenseDefinitionId = s.ExpenseDefinitionId,
                    CurrencyId = s.TryCurrencyId,
                    PaymentDate = DateTime.UtcNow,
                    Amount = 5000m, // Eşik altı -> Paid, kasa hemen düşer
                    PaymentMethod = PaymentMethod.Cash
                });
                paymentId = paid.Id;
            }

            Assert.Equal(995_000m, await GetVaultBalanceAsync(s.VaultId, s.TryCurrencyId));

            using (var ctx = TestDbContextFactory.Create())
            {
                var ownerService = CreateService(ctx, s.OwnerUserId);
                var deleted = await ownerService.DeletePaymentAsync(paymentId, "İptal");
                Assert.True(deleted);
            }

            Assert.Equal(1_000_000m, await GetVaultBalanceAsync(s.VaultId, s.TryCurrencyId));

            // İkinci kez silme denemesi false döner ve bakiyeyi tekrar değiştirmez
            using (var ctx = TestDbContextFactory.Create())
            {
                var ownerService = CreateService(ctx, s.OwnerUserId);
                var secondDelete = await ownerService.DeletePaymentAsync(paymentId, "Tekrar dene");
                Assert.False(secondDelete);
            }

            Assert.Equal(1_000_000m, await GetVaultBalanceAsync(s.VaultId, s.TryCurrencyId));
        }

        [Fact]
        public async Task GetAccessibleOfficeIdsAsync_YalnizcaAktifUserOfficeSatirlarininOfisleriniDoner()
        {
            using var ctx = TestDbContextFactory.Create();

            var officeA = new Office { Id = Guid.NewGuid(), OfficeName = "Ofis A " + Guid.NewGuid() };
            var officeB = new Office { Id = Guid.NewGuid(), OfficeName = "Ofis B " + Guid.NewGuid() };
            var officeC = new Office { Id = Guid.NewGuid(), OfficeName = "Ofis C " + Guid.NewGuid() };
            var user = new User { Id = Guid.NewGuid(), Username = "multiofis_" + Guid.NewGuid(), Password = "", Mail = "", Firstname = "Test", Lastname = "User", Rank = Rank.Staff };

            ctx.Offices.AddRange(officeA, officeB, officeC);
            ctx.Users.Add(user);
            ctx.User_Offices.Add(new User_Office { Id = Guid.NewGuid(), UserId = user.Id, OfficeId = officeA.Id, Role = OfficeRole.Cashier, IsActive = true });
            ctx.User_Offices.Add(new User_Office { Id = Guid.NewGuid(), UserId = user.Id, OfficeId = officeB.Id, Role = OfficeRole.Cashier, IsActive = true });
            ctx.User_Offices.Add(new User_Office { Id = Guid.NewGuid(), UserId = user.Id, OfficeId = officeC.Id, Role = OfficeRole.Cashier, IsActive = false }); // pasif
            await ctx.SaveChangesAsync();

            var validationService = TestAuthHelper.CreateValidationService(user.Id, ctx);
            var accessibleIds = await validationService.GetAccessibleOfficeIdsAsync();

            Assert.Equal(2, accessibleIds.Count);
            Assert.Contains(officeA.Id, accessibleIds);
            Assert.Contains(officeB.Id, accessibleIds);
            Assert.DoesNotContain(officeC.Id, accessibleIds);
        }

        [Fact]
        public async Task GetPendingApprovalsForOfficesAsync_YalnizcaVerilenOfislerinBekleyenlerinDoner()
        {
            var officeA = await SeedAsync();
            var officeB = await SeedAsync();

            using (var ctx = TestDbContextFactory.Create())
            {
                var serviceA = CreateService(ctx, officeA.StaffUserId);
                await serviceA.CreatePaymentAsync(new rm_expensepayment
                {
                    ExpenseDefinitionId = officeA.ExpenseDefinitionId,
                    CurrencyId = officeA.TryCurrencyId,
                    PaymentDate = DateTime.UtcNow,
                    Amount = 15000m, // Eşik üstü -> Pending
                    PaymentMethod = PaymentMethod.Cash
                });
            }

            using (var ctx = TestDbContextFactory.Create())
            {
                var serviceB = CreateService(ctx, officeB.StaffUserId);
                await serviceB.CreatePaymentAsync(new rm_expensepayment
                {
                    ExpenseDefinitionId = officeB.ExpenseDefinitionId,
                    CurrencyId = officeB.TryCurrencyId,
                    PaymentDate = DateTime.UtcNow,
                    Amount = 15000m,
                    PaymentMethod = PaymentMethod.Cash
                });
            }

            using var queryCtx = TestDbContextFactory.Create();
            var service = CreateService(queryCtx, officeA.OwnerUserId);
            var result = await service.GetPendingApprovalsForOfficesAsync(new List<Guid> { officeA.OfficeId });

            Assert.Single(result);
            Assert.All(result, p => Assert.Equal(officeA.ExpenseDefinitionId, p.ExpenseDefinitionId));
        }
    }
}
