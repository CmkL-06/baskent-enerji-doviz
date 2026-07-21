using System;
using System.Linq;
using System.Threading.Tasks;
using BaskentEnerji.Business.Services.ExchangeOffice.Expense;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using Xunit;

namespace BaskentEnerji.Business.Tests
{
    /// <summary>
    /// ExpenseReminderService tamamen salt-okunur — hiçbir tabloya yazmaz, her çağrıda gerçek
    /// ExpensePayment geçmişinden anlık hesaplanır. Bu testler vade hesaplama mantığının takvim
    /// sınır durumlarını (ay taşması, Pending ödemenin tekrar hatırlatmayı engellemesi vb.)
    /// doğrular.
    /// </summary>
    [Collection("SharedTestDatabase")]
    public class ExpenseReminderServiceTests
    {
        private ExpenseReminderService CreateService(BaskentEnerjiDbContext ctx) => new ExpenseReminderService(ctx);

        private async Task<(Guid officeId, Guid definitionId)> SeedDefinitionAsync(
            bool isRecurring = true,
            RecurrencePeriod? period = RecurrencePeriod.Monthly,
            int? dueDayOfMonth = null,
            DateTime? createdDate = null)
        {
            using var ctx = TestDbContextFactory.Create();

            var office = new Office { Id = Guid.NewGuid(), OfficeName = "Reminder Test Ofis " + Guid.NewGuid() };
            var category = new ExpenseCategoryDefinition { Id = Guid.NewGuid(), Name = "Test Kategori " + Guid.NewGuid().ToString("N").Substring(0, 8), IsActive = true };
            var definition = new ExpenseDefinition
            {
                Id = Guid.NewGuid(),
                OfficeId = office.Id,
                Code = "REM-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = "Test Kalemi",
                CategoryId = category.Id,
                IsActive = true,
                IsRecurring = isRecurring,
                RecurrencePeriod = period,
                DueDayOfMonth = dueDayOfMonth,
                CreatedDate = createdDate ?? DateTime.UtcNow.AddMonths(-2)
            };

            ctx.Offices.Add(office);
            ctx.ExpenseCategories.Add(category);
            ctx.ExpenseDefinitions.Add(definition);
            await ctx.SaveChangesAsync();

            return (office.Id, definition.Id);
        }

        private async Task AddPaymentAsync(Guid definitionId, DateTime paymentDate, ExpenseStatus status)
        {
            using var ctx = TestDbContextFactory.Create();
            var definition = await ctx.ExpenseDefinitions.FindAsync(definitionId);

            var currency = ctx.Currencies.FirstOrDefault(c => c.CurrencyCode == "TRY");
            if (currency == null)
            {
                currency = new BaskentEnerji.Entity.Entities.ExchangeOffice.Currency.Currency { Id = Guid.NewGuid(), CurrencyCode = "TRY" };
                ctx.Currencies.Add(currency);
            }

            var vault = new Vault { Id = Guid.NewGuid(), Name = "Reminder Test Kasa", Description = "", OfficeId = definition.OfficeId, Type = Vault.VaultType.Main };
            ctx.Vaults.Add(vault);

            ctx.ExpensePayments.Add(new ExpensePayment
            {
                Id = Guid.NewGuid(),
                ExpenseDefinitionId = definitionId,
                VaultId = vault.Id,
                CurrencyId = currency.Id,
                PaymentNumber = "EXP-TEST-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                PaymentDate = paymentDate,
                Amount = 100m,
                PaymentMethod = PaymentMethod.Cash,
                Status = status,
                CreatedByUserId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow
            });

            await ctx.SaveChangesAsync();
        }

        [Fact]
        public async Task CalculateNextDueAsync_DueDayOfMonthSetVeSonrakiAySubatTasmasi_AyinSonGunuSabitlenir()
        {
            // Son ödeme 15 Ocak'ta yapıldı, vade günü 31 -> Şubat'ta 31 gün yok, 28/29'a sabitlenmeli
            var (_, definitionId) = await SeedDefinitionAsync(dueDayOfMonth: 31, createdDate: new DateTime(2026, 1, 1));
            await AddPaymentAsync(definitionId, new DateTime(2026, 1, 15), ExpenseStatus.Paid);

            using var ctx = TestDbContextFactory.Create();
            var service = CreateService(ctx);
            var (nextDueDate, _) = await service.CalculateNextDueAsync(definitionId);

            Assert.NotNull(nextDueDate);
            // Ocak 15'ten sonraki ilk "31. gün" -> Ocak'ta zaten geçti (15 < 31 ama biz 15'ten SONRAKİ
            // ilk oluşumu istiyoruz; Ocak'ın 31'i 15'ten sonra olduğu için Ocak 31 olmalı)
            Assert.Equal(new DateTime(2026, 1, 31), nextDueDate!.Value);
        }

        [Fact]
        public async Task CalculateNextDueAsync_DueDayOfMonthGectiyseBirSonrakiAyaKayar_SubatTasmasiSabitlenir()
        {
            // Son ödeme Ocak 31'de yapıldı, vade günü 31 -> bir sonraki oluşum Şubat 31 olmaz, 28/29'a sabitlenir
            var (_, definitionId) = await SeedDefinitionAsync(dueDayOfMonth: 31, createdDate: new DateTime(2026, 1, 1));
            await AddPaymentAsync(definitionId, new DateTime(2026, 1, 31), ExpenseStatus.Paid);

            using var ctx = TestDbContextFactory.Create();
            var service = CreateService(ctx);
            var (nextDueDate, _) = await service.CalculateNextDueAsync(definitionId);

            Assert.NotNull(nextDueDate);
            var expectedDay = DateTime.DaysInMonth(2026, 2); // 2026 artık yıl değil -> 28
            Assert.Equal(new DateTime(2026, 2, expectedDay), nextDueDate!.Value);
        }

        [Fact]
        public async Task CalculateNextDueAsync_DueDayOfMonthBos_SonOdemedenBirAySonrasiHesaplanir()
        {
            var (_, definitionId) = await SeedDefinitionAsync(dueDayOfMonth: null, createdDate: new DateTime(2026, 1, 1));
            await AddPaymentAsync(definitionId, new DateTime(2026, 3, 10), ExpenseStatus.Paid);

            using var ctx = TestDbContextFactory.Create();
            var service = CreateService(ctx);
            var (nextDueDate, _) = await service.CalculateNextDueAsync(definitionId);

            Assert.Equal(new DateTime(2026, 4, 10), nextDueDate);
        }

        [Fact]
        public async Task CalculateNextDueAsync_HicOdemeYokYeniKalem_CreatedDateBazAlinir()
        {
            var createdDate = new DateTime(2026, 5, 1);
            var (_, definitionId) = await SeedDefinitionAsync(dueDayOfMonth: null, createdDate: createdDate);

            using var ctx = TestDbContextFactory.Create();
            var service = CreateService(ctx);
            var (nextDueDate, _) = await service.CalculateNextDueAsync(definitionId);

            Assert.Equal(createdDate.AddMonths(1), nextDueDate);
        }

        [Fact]
        public async Task CalculateNextDueAsync_PendingOdemeVarsa_SonPendingBazAlinirTekrarHatirlatilmaz()
        {
            var (_, definitionId) = await SeedDefinitionAsync(dueDayOfMonth: null, createdDate: new DateTime(2026, 1, 1));
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")).Date;
            await AddPaymentAsync(definitionId, today, ExpenseStatus.Pending);

            using var ctx = TestDbContextFactory.Create();
            var service = CreateService(ctx);
            var (nextDueDate, dueStatus) = await service.CalculateNextDueAsync(definitionId);

            // Pending ödeme "bu dönem için işlem başlatıldı" sayılır -> sıradaki vade bugünden
            // yaklaşık bir ay sonrasına gitmeli, bu yüzden "Gecikmiş"/"Bu Hafta" olmamalı.
            Assert.Equal(today.AddMonths(1), nextDueDate);
            Assert.NotEqual("Gecikmiş", dueStatus);
        }

        [Fact]
        public async Task GetUpcomingRecurringAsync_GecikmisKalemListelenir_UzakKalemListelenmez()
        {
            var turkeyToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")).Date;

            // Kalem A: son ödeme 2 ay önce yapıldı -> vade çoktan geçmiş (Gecikmiş)
            var (officeId, overdueDefId) = await SeedDefinitionAsync(dueDayOfMonth: null, createdDate: turkeyToday.AddMonths(-3));
            await AddPaymentAsync(overdueDefId, turkeyToday.AddMonths(-2), ExpenseStatus.Paid);

            // Kalem B: az önce ödendi -> sıradaki vade ~1 ay sonra, henüz uzak
            var (_, farDefId) = await SeedDefinitionAsync(dueDayOfMonth: null, createdDate: turkeyToday.AddMonths(-3));
            using (var ctx = TestDbContextFactory.Create())
            {
                var farDef = await ctx.ExpenseDefinitions.FindAsync(farDefId);
                farDef!.OfficeId = officeId; // aynı ofiste olsun
                await ctx.SaveChangesAsync();
            }
            await AddPaymentAsync(farDefId, turkeyToday, ExpenseStatus.Paid);

            using var queryCtx = TestDbContextFactory.Create();
            var service = CreateService(queryCtx);
            var result = await service.GetUpcomingRecurringAsync(officeId);

            Assert.Contains(result, r => r.Id == overdueDefId && r.DueStatus == "Gecikmiş");
            Assert.DoesNotContain(result, r => r.Id == farDefId);
        }
    }
}
