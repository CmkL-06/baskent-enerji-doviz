using System;
using System.Threading.Tasks;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.ExchangeOffice.Office;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BaskentEnerji.Business.Tests
{
    /// <summary>
    /// Gerçek SQLEXPRESS üzerinde izole bir test veritabanı (BaskentEnerjiTests) kullanır —
    /// WacService.GetOrCreateWacAsync raw SQL (WITH UPDLOCK) kullandığı için InMemory provider
    /// desteklemiyor. Her test kendi rastgele Vault/Currency çiftini oluşturur, böylece testler
    /// birbirinin verisiyle çakışmaz ve temizlik gerekmez (veriler test DB'sinde kalır, zararsız).
    /// </summary>
    [Collection("SharedTestDatabase")]
    public class WacServiceTests
    {
        private async Task<(Guid vaultId, Guid currencyId)> SeedVaultAndCurrencyAsync()
        {
            using var ctx = TestDbContextFactory.Create();

            var office = new Office { Id = Guid.NewGuid(), OfficeName = "Test Ofis " + Guid.NewGuid() };
            var currency = new Currency { Id = Guid.NewGuid(), CurrencyCode = "TST" + Guid.NewGuid().ToString("N").Substring(0, 4) };
            var vault = new Vault { Id = Guid.NewGuid(), Name = "Test Kasa", Description = "", OfficeId = office.Id };

            ctx.Offices.Add(office);
            ctx.Currencies.Add(currency);
            ctx.Vaults.Add(vault);
            await ctx.SaveChangesAsync();

            return (vault.Id, currency.Id);
        }

        [Fact]
        public async Task RecalculateWacOnPurchaseAsync_IlkAlim_WacSatinAlmaKuruOlur()
        {
            var (vaultId, currencyId) = await SeedVaultAndCurrencyAsync();
            using var ctx = TestDbContextFactory.Create();
            var wacService = new WacService(ctx);

            await wacService.RecalculateWacOnPurchaseAsync(vaultId, currencyId, purchaseAmount: 100, purchaseRate: 40m);

            var wac = await wacService.GetWacAsync(vaultId, currencyId);
            Assert.Equal(40m, wac);
        }

        [Fact]
        public async Task RecalculateWacOnPurchaseAsync_IkinciAlim_AgirlikliOrtalamaHesaplanir()
        {
            var (vaultId, currencyId) = await SeedVaultAndCurrencyAsync();
            using var ctx = TestDbContextFactory.Create();
            var wacService = new WacService(ctx);

            // 100 birim @ 40 + 100 birim @ 50 => toplam maliyet 9000 / 200 birim = 45 WAC
            await wacService.RecalculateWacOnPurchaseAsync(vaultId, currencyId, 100, 40m);
            await wacService.RecalculateWacOnPurchaseAsync(vaultId, currencyId, 100, 50m);

            var wac = await wacService.GetWacAsync(vaultId, currencyId);
            Assert.Equal(45m, wac);
        }

        [Fact]
        public async Task CalculateRealizedProfitAsync_SatisKuruWacinUzerinde_PozitifKarVerir()
        {
            var (vaultId, currencyId) = await SeedVaultAndCurrencyAsync();
            using var ctx = TestDbContextFactory.Create();
            var wacService = new WacService(ctx);

            await wacService.RecalculateWacOnPurchaseAsync(vaultId, currencyId, 100, 40m);

            // WAC 40 iken 45'ten 50 birim satılırsa: (45-40)*50 = 250 kar
            var profit = await wacService.CalculateRealizedProfitAsync(sellRate: 45m, quantity: 50, vaultId, currencyId);
            Assert.Equal(250m, profit);
        }

        [Fact]
        public async Task CalculateRealizedProfitAsync_SatisKuruWacinAltinda_NegatifKarVerir()
        {
            var (vaultId, currencyId) = await SeedVaultAndCurrencyAsync();
            using var ctx = TestDbContextFactory.Create();
            var wacService = new WacService(ctx);

            await wacService.RecalculateWacOnPurchaseAsync(vaultId, currencyId, 100, 40m);

            // WAC 40 iken 35'ten 50 birim satılırsa: (35-40)*50 = -250 (zarar)
            var profit = await wacService.CalculateRealizedProfitAsync(sellRate: 35m, quantity: 50, vaultId, currencyId);
            Assert.Equal(-250m, profit);
        }

        [Fact]
        public async Task CalculateRealizedProfitAsync_WacSifirsa_SifirDonerBolmeHatasiOlmaz()
        {
            // Hiç alım yapılmamış (WAC=0) bir para biriminde satış denenirse, WacService.cs:58
            // "if (wacEntity.Wac == 0) return 0;" korumasıyla sıfır bölme/anlamsız kâr oluşmamalı.
            var (vaultId, currencyId) = await SeedVaultAndCurrencyAsync();
            using var ctx = TestDbContextFactory.Create();
            var wacService = new WacService(ctx);

            var profit = await wacService.CalculateRealizedProfitAsync(sellRate: 45m, quantity: 50, vaultId, currencyId);
            Assert.Equal(0m, profit);
        }

        [Fact]
        public async Task AdjustWacQuantityAsync_NegatifMiktarSifiraSabitlenir()
        {
            // WacService.cs:67 "if (newQuantity < 0) newQuantity = 0;" korumasını doğrular —
            // hesaplama hatası nedeniyle negatif miktar gelirse kasa negatif stoğa düşmemeli.
            var (vaultId, currencyId) = await SeedVaultAndCurrencyAsync();
            using var ctx = TestDbContextFactory.Create();
            var wacService = new WacService(ctx);

            await wacService.RecalculateWacOnPurchaseAsync(vaultId, currencyId, 100, 40m);
            await wacService.AdjustWacQuantityAsync(vaultId, currencyId, newQuantity: -10, WacAdjustReason.Sale);

            using var verifyCtx = TestDbContextFactory.Create();
            var wacRow = await verifyCtx.CurrencyWacs.FindAsync(
                (await verifyCtx.CurrencyWacs.FirstAsync(w => w.VaultId == vaultId && w.CurrencyId == currencyId)).Id);
            Assert.Equal(0m, wacRow!.Quantity);
            Assert.Equal(0m, wacRow.Wac); // Miktar sıfırlanınca WAC de sıfırlanır (WacService.cs:72-73)
        }
    }
}
