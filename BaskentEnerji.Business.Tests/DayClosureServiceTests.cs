using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Services.ExchangeOffice.Office;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Entities.User;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BaskentEnerji.Business.Tests
{
    /// <summary>
    /// 500 TL onay eşiği ve gün kapanışı zorunluluk mantığı için testler (Faz 149-151'de eklenen
    /// özellik). Her test kendi Office/Vault/Currency/User setini oluşturur.
    /// </summary>
    [Collection("SharedTestDatabase")]
    public class DayClosureServiceTests
    {
        private class TestScenario
        {
            public Guid OfficeId;
            public Guid VaultId;
            public Guid TryCurrencyId;
            public Guid UsdCurrencyId;
            public Guid OwnerUserId;
            public Guid StaffUserId;
        }

        private async Task<TestScenario> SeedAsync()
        {
            using var ctx = TestDbContextFactory.Create();

            var office = new Office { Id = Guid.NewGuid(), OfficeName = "Test Ofis " + Guid.NewGuid() };
            var vault = new Vault { Id = Guid.NewGuid(), Name = "Test Kasa", Description = "", OfficeId = office.Id, Type = Vault.VaultType.Main };
            var usdCurrency = new Currency { Id = Guid.NewGuid(), CurrencyCode = "TST" + Guid.NewGuid().ToString("N").Substring(0, 4) };
            var owner = new User { Id = Guid.NewGuid(), Username = "owner_" + Guid.NewGuid(), Password = "", Mail = "", Firstname = "Test", Lastname = "Owner", Rank = Rank.Owner };
            var staff = new User { Id = Guid.NewGuid(), Username = "staff_" + Guid.NewGuid(), Password = "", Mail = "", Firstname = "Test", Lastname = "Staff", Rank = Rank.Staff };

            // TRY, üretimde olduğu gibi tekil/global bir referans para birimi olmalı — paylaşılan test
            // veritabanında her test kendi "TRY" satırını oluşturursa, ZReportService gibi CurrencyCode'a
            // göre global sözlük kuran servisler "duplicate key" hatası alır. Var olanı yeniden kullan.
            var tryCurrency = await ctx.Currencies.FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
            if (tryCurrency == null)
            {
                tryCurrency = new Currency { Id = Guid.NewGuid(), CurrencyCode = "TRY" };
                ctx.Currencies.Add(tryCurrency);
            }

            ctx.Offices.Add(office);
            ctx.Vaults.Add(vault);
            ctx.Currencies.Add(usdCurrency);
            ctx.Users.AddRange(owner, staff);
            await ctx.SaveChangesAsync();

            return new TestScenario
            {
                OfficeId = office.Id,
                VaultId = vault.Id,
                TryCurrencyId = tryCurrency.Id,
                UsdCurrencyId = usdCurrency.Id,
                OwnerUserId = owner.Id,
                StaffUserId = staff.Id
            };
        }

        private DayClosureService CreateService(BaskentEnerjiDbContext ctx, Guid actingUserId)
        {
            var wacService = new WacService(ctx);
            var validationService = TestAuthHelper.CreateValidationService(actingUserId, ctx);
            return new DayClosureService(ctx, wacService, validationService);
        }

        [Fact]
        public async Task CloseDayAsync_500TLAltiFark_DogrudanKapanirVeBakiyeUygulanir()
        {
            var s = await SeedAsync();
            using var ctx = TestDbContextFactory.Create();
            var service = CreateService(ctx, s.StaffUserId);

            // Sistemde 0 TRY var, fiziksel sayımda 100 TRY bulundu -> fark 100 TL, eşiğin (500) altında.
            var result = await service.CloseDayAsync(new rm_dayclosure
            {
                OfficeId = s.OfficeId,
                BusinessDate = DateTime.UtcNow.Date,
                Notes = "test",
                Details = new List<rm_dayclosuredetail>
                {
                    new rm_dayclosuredetail { CurrencyId = s.TryCurrencyId, PhysicalCount = 100m, DiscrepancyNote = "sayım farkı" }
                }
            });

            Assert.Equal(DayClosureStatus.Closed, result.Status);

            using var verifyCtx = TestDbContextFactory.Create();
            var balance = await verifyCtx.VaultBalances.FirstOrDefaultAsync(b => b.VaultId == s.VaultId && b.CurrencyId == s.TryCurrencyId);
            Assert.NotNull(balance);
            Assert.Equal(100m, balance!.Balance); // Fark hemen uygulanmış olmalı (eşik altı)
        }

        [Fact]
        public async Task CloseDayAsync_500TLUstuFark_OnayBekletirVeBakiyeUygulanmaz()
        {
            var s = await SeedAsync();

            // Önce USD için WAC=40 oluştur (rate-to-TRY hesaplaması bunu kullanacak).
            using (var wacCtx = TestDbContextFactory.Create())
            {
                var wacService = new WacService(wacCtx);
                await wacService.RecalculateWacOnPurchaseAsync(s.VaultId, s.UsdCurrencyId, 100, 40m);
            }

            using var ctx = TestDbContextFactory.Create();
            var service = CreateService(ctx, s.StaffUserId);

            // Sistemde 0 USD, fiziksel sayımda 20 USD bulundu -> fark 20 * WAC(40) = 800 TL, eşiğin üstünde.
            var result = await service.CloseDayAsync(new rm_dayclosure
            {
                OfficeId = s.OfficeId,
                BusinessDate = DateTime.UtcNow.Date,
                Notes = "test",
                Details = new List<rm_dayclosuredetail>
                {
                    new rm_dayclosuredetail { CurrencyId = s.UsdCurrencyId, PhysicalCount = 20m, DiscrepancyNote = "büyük fark" }
                }
            });

            Assert.Equal(DayClosureStatus.PendingApproval, result.Status);

            using var verifyCtx = TestDbContextFactory.Create();
            var balance = await verifyCtx.VaultBalances.FirstOrDefaultAsync(b => b.VaultId == s.VaultId && b.CurrencyId == s.UsdCurrencyId);
            // Bakiye satırı ya hiç yok ya da 0 olmalı — onay bekleyen fark HENÜZ uygulanmamış olmalı.
            Assert.True(balance == null || balance.Balance == 0m);
        }

        [Fact]
        public async Task CanTransactAsync_OnayBekleyenKapanisVarken_SonrakiGunBloklu()
        {
            var s = await SeedAsync();
            using (var wacCtx = TestDbContextFactory.Create())
            {
                var wacService = new WacService(wacCtx);
                await wacService.RecalculateWacOnPurchaseAsync(s.VaultId, s.UsdCurrencyId, 100, 40m);
            }

            using (var ctx = TestDbContextFactory.Create())
            {
                var service = CreateService(ctx, s.StaffUserId);
                await service.CloseDayAsync(new rm_dayclosure
                {
                    OfficeId = s.OfficeId,
                    BusinessDate = DateTime.UtcNow.Date.AddDays(-1),
                    Details = new List<rm_dayclosuredetail>
                    {
                        new rm_dayclosuredetail { CurrencyId = s.UsdCurrencyId, PhysicalCount = 20m, DiscrepancyNote = "büyük fark" }
                    }
                });
            }

            using var verifyCtx = TestDbContextFactory.Create();
            var verifyService = CreateService(verifyCtx, s.StaffUserId);
            var canTransact = await verifyService.CanTransactAsync(s.OfficeId);

            // PendingApproval "gerçekten kapanmış" sayılmamalı -> bir sonraki gün (bugün) hâlâ bloklu.
            Assert.False(canTransact);
        }

        [Fact]
        public async Task ApproveDayClosureAsync_OwnerOnaylarsa_BakiyeUygulanirVeKapanirTamamlanir()
        {
            var s = await SeedAsync();
            using (var wacCtx = TestDbContextFactory.Create())
            {
                var wacService = new WacService(wacCtx);
                await wacService.RecalculateWacOnPurchaseAsync(s.VaultId, s.UsdCurrencyId, 100, 40m);
            }

            Guid closureId;
            using (var ctx = TestDbContextFactory.Create())
            {
                var service = CreateService(ctx, s.StaffUserId);
                var result = await service.CloseDayAsync(new rm_dayclosure
                {
                    OfficeId = s.OfficeId,
                    BusinessDate = DateTime.UtcNow.Date,
                    Details = new List<rm_dayclosuredetail>
                    {
                        new rm_dayclosuredetail { CurrencyId = s.UsdCurrencyId, PhysicalCount = 20m, DiscrepancyNote = "büyük fark" }
                    }
                });
                closureId = result.Id;
            }

            using (var ownerCtx = TestDbContextFactory.Create())
            {
                var ownerService = CreateService(ownerCtx, s.OwnerUserId);
                var approved = await ownerService.ApproveDayClosureAsync(closureId, approve: true, rejectionNote: null!);
                Assert.Equal(DayClosureStatus.Closed, approved.Status);
            }

            using var verifyCtx = TestDbContextFactory.Create();
            var balance = await verifyCtx.VaultBalances.FirstOrDefaultAsync(b => b.VaultId == s.VaultId && b.CurrencyId == s.UsdCurrencyId);
            Assert.NotNull(balance);
            Assert.Equal(20m, balance!.Balance); // Onaydan sonra fark artık uygulanmış olmalı

            var verifyService = CreateService(verifyCtx, s.StaffUserId);
            Assert.True(await verifyService.CanTransactAsync(s.OfficeId)); // Onaylandıktan sonra artık bloklamamalı
        }

        [Fact]
        public async Task ApproveDayClosureAsync_OwnerOlmayanReddedilir()
        {
            var s = await SeedAsync();
            using (var wacCtx = TestDbContextFactory.Create())
            {
                var wacService = new WacService(wacCtx);
                await wacService.RecalculateWacOnPurchaseAsync(s.VaultId, s.UsdCurrencyId, 100, 40m);
            }

            Guid closureId;
            using (var ctx = TestDbContextFactory.Create())
            {
                var service = CreateService(ctx, s.StaffUserId);
                var result = await service.CloseDayAsync(new rm_dayclosure
                {
                    OfficeId = s.OfficeId,
                    BusinessDate = DateTime.UtcNow.Date,
                    Details = new List<rm_dayclosuredetail>
                    {
                        new rm_dayclosuredetail { CurrencyId = s.UsdCurrencyId, PhysicalCount = 20m, DiscrepancyNote = "büyük fark" }
                    }
                });
                closureId = result.Id;
            }

            using var staffCtx = TestDbContextFactory.Create();
            var staffService = CreateService(staffCtx, s.StaffUserId); // Owner değil, sıradan personel

            await Assert.ThrowsAsync<ApiException>(() =>
                staffService.ApproveDayClosureAsync(closureId, approve: true, rejectionNote: null!));
        }

        [Fact]
        public async Task ApproveDayClosureAsync_Reddedilirse_BakiyeDegismezVeAyniGunTekrarKapatilabilir()
        {
            var s = await SeedAsync();
            using (var wacCtx = TestDbContextFactory.Create())
            {
                var wacService = new WacService(wacCtx);
                await wacService.RecalculateWacOnPurchaseAsync(s.VaultId, s.UsdCurrencyId, 100, 40m);
            }

            var businessDate = DateTime.UtcNow.Date;
            Guid closureId;
            using (var ctx = TestDbContextFactory.Create())
            {
                var service = CreateService(ctx, s.StaffUserId);
                var result = await service.CloseDayAsync(new rm_dayclosure
                {
                    OfficeId = s.OfficeId,
                    BusinessDate = businessDate,
                    Details = new List<rm_dayclosuredetail>
                    {
                        new rm_dayclosuredetail { CurrencyId = s.UsdCurrencyId, PhysicalCount = 20m, DiscrepancyNote = "büyük fark" }
                    }
                });
                closureId = result.Id;
            }

            using (var ownerCtx = TestDbContextFactory.Create())
            {
                var ownerService = CreateService(ownerCtx, s.OwnerUserId);
                var rejected = await ownerService.ApproveDayClosureAsync(closureId, approve: false, rejectionNote: "yanlış sayım");
                Assert.Equal(DayClosureStatus.Rejected, rejected.Status);
            }

            using (var verifyCtx = TestDbContextFactory.Create())
            {
                var balance = await verifyCtx.VaultBalances.FirstOrDefaultAsync(b => b.VaultId == s.VaultId && b.CurrencyId == s.UsdCurrencyId);
                Assert.True(balance == null || balance.Balance == 0m);
            }

            // Reddedilen kapanış artık "gerçek" sayılmadığından, aynı iş günü tekrar kapatılabilmeli.
            using var retryCtx = TestDbContextFactory.Create();
            var retryService = CreateService(retryCtx, s.StaffUserId);
            var retryResult = await retryService.CloseDayAsync(new rm_dayclosure
            {
                OfficeId = s.OfficeId,
                BusinessDate = businessDate,
                Details = new List<rm_dayclosuredetail>
                {
                    new rm_dayclosuredetail { CurrencyId = s.UsdCurrencyId, PhysicalCount = 0m, DiscrepancyNote = "" }
                }
            });
            Assert.Equal(DayClosureStatus.Closed, retryResult.Status);
        }

        [Fact]
        public async Task GetDayStatusAsync_GununIlkIslemi_AcanKisiVeToplamlarDogruDoner()
        {
            var s = await SeedAsync();

            using (var ctx = TestDbContextFactory.Create())
            {
                var earlierTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    TransactionNumber = "T-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    VaultId = s.VaultId,
                    UserId = s.StaffUserId,
                    Type = TransactionType.Exchange,
                    TransactionDate = DateTime.UtcNow.Date.AddHours(9),
                    Status = TransactionStatus.Completed,
                    Profit = 0,
                    Details = new List<TransactionDetail>
                    {
                        new TransactionDetail { Id = Guid.NewGuid(), CurrencyId = s.UsdCurrencyId, Side = TransactionSide.Debit, Amount = 10m, Rate = 40m, Commission = 0, NetAmount = 10m },
                        new TransactionDetail { Id = Guid.NewGuid(), CurrencyId = s.TryCurrencyId, Side = TransactionSide.Credit, Amount = 400m, Rate = 1m, Commission = 0, NetAmount = 400m }
                    }
                };

                var laterTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    TransactionNumber = "T-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    VaultId = s.VaultId,
                    UserId = s.OwnerUserId,
                    Type = TransactionType.Exchange,
                    TransactionDate = DateTime.UtcNow.Date.AddHours(15),
                    Status = TransactionStatus.Completed,
                    Profit = 0,
                    Details = new List<TransactionDetail>
                    {
                        new TransactionDetail { Id = Guid.NewGuid(), CurrencyId = s.UsdCurrencyId, Side = TransactionSide.Debit, Amount = 5m, Rate = 40m, Commission = 0, NetAmount = 5m },
                        new TransactionDetail { Id = Guid.NewGuid(), CurrencyId = s.TryCurrencyId, Side = TransactionSide.Credit, Amount = 200m, Rate = 1m, Commission = 0, NetAmount = 200m }
                    }
                };

                ctx.Transactions.AddRange(earlierTransaction, laterTransaction);
                await ctx.SaveChangesAsync();
            }

            using var verifyCtx = TestDbContextFactory.Create();
            var service = CreateService(verifyCtx, s.StaffUserId);
            var status = await service.GetDayStatusAsync(s.OfficeId);

            // Günün ilk işlemini yapan (saat 09) personel Staff — Owner'ın daha geç (saat 15) yaptığı
            // işlem "açan kişi" olarak sayılmamalı.
            Assert.Equal("Test Staff", status.OpenedByUserName);
            Assert.Equal(2, status.TotalTransactionCount);
            // Her işlemde tek yabancı bacak (USD) var: 10*40 + 5*40 = 600 TL toplam hacim.
            Assert.Equal(600m, status.TotalTransactionVolumeInTRY);
        }
    }
}
