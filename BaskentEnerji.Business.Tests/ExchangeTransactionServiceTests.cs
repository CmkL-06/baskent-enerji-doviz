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
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace BaskentEnerji.Business.Tests
{
    /// <summary>
    /// ExchangeTransactionService.ProcessExchangeAsync testleri — sistemin en kritik akışı: WAC
    /// güncelleme (alış), gerçekleşen kâr hesaplama (satış), yetersiz bakiye koruması, gün kapanışı
    /// zorunluluğu gate'i ve arbitraj (çapraz kur) hesaplaması. Gerçek VaultService/WacService/
    /// DayClosureService kullanılıyor (raw SQL UPDLOCK gerektiğinden InMemory provider uygun değil —
    /// bkz. TestDbContextFactory üstündeki not). IMapper/PartyTransactionIntegration bu metodun
    /// dokunmadığı kod yollarında olduğundan null geçiliyor.
    /// </summary>
    [Collection("SharedTestDatabase")]
    public class ExchangeTransactionServiceTests
    {
        private ExchangeTransactionService CreateService(BaskentEnerjiDbContext ctx, Guid actingUserId)
        {
            var wacService = new WacService(ctx);
            var validationService = TestAuthHelper.CreateValidationService(actingUserId, ctx);
            var dayClosureService = new DayClosureService(ctx, wacService, validationService);
            var vaultService = new VaultService(ctx, null!, new MemoryCache(new MemoryCacheOptions()), validationService, wacService);
            return new ExchangeTransactionService(ctx, null!, vaultService, wacService, validationService, null!, new MemoryCache(new MemoryCacheOptions()), dayClosureService);
        }

        private class TestScenario
        {
            public Guid OfficeId;
            public Guid VaultId;
            public Guid TryId;
            public Guid ForeignId;
            public Guid OwnerUserId;
        }

        private async Task<TestScenario> SeedAsync()
        {
            using var ctx = TestDbContextFactory.Create();

            var office = new Office { Id = Guid.NewGuid(), OfficeName = "ExchangeTx Test Ofis " + Guid.NewGuid() };
            var vault = new Vault { Id = Guid.NewGuid(), Name = "Test Kasa", Description = "", OfficeId = office.Id, Type = Vault.VaultType.Main };

            var tryCurrency = await ctx.Currencies.FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
            if (tryCurrency == null)
            {
                tryCurrency = new Currency { Id = Guid.NewGuid(), CurrencyCode = "TRY" };
                ctx.Currencies.Add(tryCurrency);
            }
            var foreignCurrency = new Currency { Id = Guid.NewGuid(), CurrencyCode = "TST" + Guid.NewGuid().ToString("N").Substring(0, 4) };

            // Owner rank kullanılıyor çünkü EnsureNotViewerAsync, Owner/Admin için doğrudan geçiyor
            // (IsAdminAsync ile) — aksi halde GetOfficeRoleAsync, test şemasında olmayan User_Offices
            // tablosunu sorgulamaya çalışır.
            var owner = new User { Id = Guid.NewGuid(), Username = "owner_" + Guid.NewGuid(), Password = "", Mail = "", Firstname = "Test", Lastname = "Owner", Rank = Rank.Owner };

            ctx.Offices.Add(office);
            ctx.Vaults.Add(vault);
            ctx.Currencies.Add(foreignCurrency);
            ctx.Users.Add(owner);
            await ctx.SaveChangesAsync();

            return new TestScenario
            {
                OfficeId = office.Id,
                VaultId = vault.Id,
                TryId = tryCurrency.Id,
                ForeignId = foreignCurrency.Id,
                OwnerUserId = owner.Id
            };
        }

        private async Task AddRateAsync(Guid officeId, Guid sourceId, Guid targetId, decimal buy, decimal sell)
        {
            using var ctx = TestDbContextFactory.Create();
            ctx.ExchangeRates.Add(new ExchangeRate
            {
                Id = Guid.NewGuid(), OfficeId = officeId, SourceCurrencyId = sourceId, TargetCurrencyId = targetId,
                BuyRate = buy, SellRate = sell, IsActive = true, EffectiveFrom = DateTime.UtcNow.AddHours(-1)
            });
            await ctx.SaveChangesAsync();
        }

        [Fact]
        public async Task ProcessExchangeAsync_MusteridenAlis_WacOlusurVeKarSifirdir()
        {
            var s = await SeedAsync();
            await AddRateAsync(s.OfficeId, s.ForeignId, s.TryId, buy: 40m, sell: 42m);

            using var ctx = TestDbContextFactory.Create();
            var service = CreateService(ctx, s.OwnerUserId);

            var results = await service.ProcessExchangeAsync(new List<rm_exchangetransaction>
            {
                new rm_exchangetransaction { VaultId = s.VaultId, SourceCurrencyId = s.ForeignId, TargetCurrencyId = s.TryId, SourceAmount = 100m, IsBuyingFromCustomer = true }
            });

            Assert.Single(results);
            Assert.Equal(0m, results[0].ProfitLoss);
            Assert.Equal(40m, results[0].AppliedRate);
            Assert.Equal(4000m, results[0].TargetAmount);

            using var verifyCtx = TestDbContextFactory.Create();
            var foreignBalance = await verifyCtx.VaultBalances.FirstOrDefaultAsync(b => b.VaultId == s.VaultId && b.CurrencyId == s.ForeignId);
            var tryBalance = await verifyCtx.VaultBalances.FirstOrDefaultAsync(b => b.VaultId == s.VaultId && b.CurrencyId == s.TryId);
            var wac = await verifyCtx.CurrencyWacs.FirstOrDefaultAsync(w => w.VaultId == s.VaultId && w.CurrencyId == s.ForeignId);

            Assert.NotNull(foreignBalance);
            Assert.Equal(100m, foreignBalance!.Balance);
            Assert.NotNull(tryBalance);
            Assert.Equal(-4000m, tryBalance!.Balance); // Müşteriye 4000 TL ödendi
            Assert.NotNull(wac);
            Assert.Equal(40m, wac!.Wac);
            Assert.Equal(100m, wac.Quantity);
        }

        [Fact]
        public async Task ProcessExchangeAsync_MusteriyeSatis_WacBazliGercekKarHesaplanir()
        {
            var s = await SeedAsync();
            await AddRateAsync(s.OfficeId, s.ForeignId, s.TryId, buy: 40m, sell: 42m);

            using (var ctx = TestDbContextFactory.Create())
            {
                var wacService = new WacService(ctx);
                await wacService.RecalculateWacOnPurchaseAsync(s.VaultId, s.ForeignId, 200m, 40m);
                // WAC (maliyet takibi) ve VaultBalance (gerçek kasa bakiyesi) ayrı tablolar — WAC
                // güncellemesi bakiyeyi otomatik oluşturmaz, gerçek akışta bunu VaultService yapar.
                ctx.VaultBalances.Add(new VaultBalance { Id = Guid.NewGuid(), VaultId = s.VaultId, CurrencyId = s.ForeignId, Balance = 200m, LastUpdated = DateTime.UtcNow });
                await ctx.SaveChangesAsync();
            }

            using var svcCtx = TestDbContextFactory.Create();
            var service = CreateService(svcCtx, s.OwnerUserId);

            var results = await service.ProcessExchangeAsync(new List<rm_exchangetransaction>
            {
                new rm_exchangetransaction { VaultId = s.VaultId, SourceCurrencyId = s.ForeignId, TargetCurrencyId = s.TryId, SourceAmount = 50m, IsBuyingFromCustomer = false }
            });

            // Kâr: (SatışKuru - WAC) * Miktar = (42-40)*50 = 100
            Assert.Equal(100m, results[0].ProfitLoss);
            Assert.Equal(2100m, results[0].TargetAmount);

            using var verifyCtx = TestDbContextFactory.Create();
            var foreignBalance = await verifyCtx.VaultBalances.FirstOrDefaultAsync(b => b.VaultId == s.VaultId && b.CurrencyId == s.ForeignId);
            var tryBalance = await verifyCtx.VaultBalances.FirstOrDefaultAsync(b => b.VaultId == s.VaultId && b.CurrencyId == s.TryId);

            Assert.Equal(150m, foreignBalance!.Balance); // 200 - 50
            Assert.Equal(2100m, tryBalance!.Balance); // Müşteriden 2100 TL alındı
        }

        [Fact]
        public async Task ProcessExchangeAsync_YetersizBakiyeyleSatis_ApiExceptionFirlatir()
        {
            var s = await SeedAsync();
            await AddRateAsync(s.OfficeId, s.ForeignId, s.TryId, buy: 40m, sell: 42m);

            using (var ctx = TestDbContextFactory.Create())
            {
                var wacService = new WacService(ctx);
                await wacService.RecalculateWacOnPurchaseAsync(s.VaultId, s.ForeignId, 10m, 40m); // sadece 10 birim var
            }

            using var svcCtx = TestDbContextFactory.Create();
            var service = CreateService(svcCtx, s.OwnerUserId);

            await Assert.ThrowsAsync<ApiException>(() => service.ProcessExchangeAsync(new List<rm_exchangetransaction>
            {
                new rm_exchangetransaction { VaultId = s.VaultId, SourceCurrencyId = s.ForeignId, TargetCurrencyId = s.TryId, SourceAmount = 50m, IsBuyingFromCustomer = false }
            }));
        }

        [Fact]
        public async Task ProcessExchangeAsync_OncekiGunKapanmamissa_IslemBloklanir()
        {
            var s = await SeedAsync();
            await AddRateAsync(s.OfficeId, s.ForeignId, s.TryId, buy: 40m, sell: 42m);

            // Eski (birden fazla gün önceki) bir kapanış -> CanTransactAsync bugünü bloklar.
            using (var ctx = TestDbContextFactory.Create())
            {
                ctx.DayClosures.Add(new DayClosure
                {
                    Id = Guid.NewGuid(),
                    OfficeId = s.OfficeId,
                    VaultId = s.VaultId,
                    BusinessDate = DateTime.UtcNow.Date.AddDays(-3),
                    ClosedByUserId = s.OwnerUserId,
                    ClosedAt = DateTime.UtcNow,
                    Status = DayClosureStatus.Closed,
                    Notes = "",
                    RejectionNote = ""
                });
                await ctx.SaveChangesAsync();
            }

            using var svcCtx = TestDbContextFactory.Create();
            var service = CreateService(svcCtx, s.OwnerUserId);

            var ex = await Assert.ThrowsAsync<ApiException>(() => service.ProcessExchangeAsync(new List<rm_exchangetransaction>
            {
                new rm_exchangetransaction { VaultId = s.VaultId, SourceCurrencyId = s.ForeignId, TargetCurrencyId = s.TryId, SourceAmount = 50m, IsBuyingFromCustomer = true }
            }));
            Assert.Contains("kapanışı yapılmadan", ex.Message);
        }

        [Fact]
        public async Task ProcessExchangeAsync_Arbitraj_IkiYabanciParaArasiDogruHesaplanir()
        {
            var s = await SeedAsync();
            var currencyBId = (await CreateSecondForeignCurrencyAsync()).Id;

            // Verilen (target) birim için yeterli kasa bakiyesi ve WAC gerekiyor.
            using (var ctx = TestDbContextFactory.Create())
            {
                var wacService = new WacService(ctx);
                await wacService.RecalculateWacOnPurchaseAsync(s.VaultId, currencyBId, 100m, 3m); // WAC=3
                ctx.VaultBalances.Add(new VaultBalance { Id = Guid.NewGuid(), VaultId = s.VaultId, CurrencyId = currencyBId, Balance = 100m, LastUpdated = DateTime.UtcNow });
                await ctx.SaveChangesAsync();
            }

            using var svcCtx = TestDbContextFactory.Create();
            var service = CreateService(svcCtx, s.OwnerUserId);

            // 100 birim ForeignId al (kur 2), karşılığında currencyB ver (kur 4) -> targetAmount = 100*2/4 = 50
            var results = await service.ProcessExchangeAsync(new List<rm_exchangetransaction>
            {
                new rm_exchangetransaction
                {
                    VaultId = s.VaultId, SourceCurrencyId = s.ForeignId, TargetCurrencyId = currencyBId,
                    SourceAmount = 100m, SourceCustomRate = 2m, TargetCustomRate = 4m
                }
            });

            Assert.Equal(50m, results[0].TargetAmount);
            // Kâr: (4-3)*50 = 50 (currencyB'nin WAC'ına göre satış kârı)
            Assert.Equal(50m, results[0].ProfitLoss);

            using var verifyCtx = TestDbContextFactory.Create();
            var foreignBalance = await verifyCtx.VaultBalances.FirstOrDefaultAsync(b => b.VaultId == s.VaultId && b.CurrencyId == s.ForeignId);
            var bBalance = await verifyCtx.VaultBalances.FirstOrDefaultAsync(b => b.VaultId == s.VaultId && b.CurrencyId == currencyBId);

            Assert.Equal(100m, foreignBalance!.Balance); // Alınan birim kasaya girdi
            Assert.Equal(50m, bBalance!.Balance); // 100 - 50 verildi
        }

        private async Task<Currency> CreateSecondForeignCurrencyAsync()
        {
            using var ctx = TestDbContextFactory.Create();
            var currency = new Currency { Id = Guid.NewGuid(), CurrencyCode = "TST" + Guid.NewGuid().ToString("N").Substring(0, 4) };
            ctx.Currencies.Add(currency);
            await ctx.SaveChangesAsync();
            return currency;
        }

        /// <summary>
        /// DENETİM RAPORU DOĞRULAMA TESTİ — hedef "RemoveTransaction'da WAC geri alma, satılan
        /// miktarın orijinal maliyetini değil güncel WAC'ı kullanıyor" bulgusuydu. Kök neden:
        /// eskiden RemoveTransaction, satış geri alımında `_context.VaultBalances....Select(vb =>
        /// vb.Balance).FirstOrDefaultAsync()` ile STALE bir bakiye okuyup bunu doğrudan
        /// `WacService.AdjustWacQuantityAsync`'e geçiriyordu — bu metod da yalnızca Quantity'yi
        /// günceller, Wac alanını hiçbir zaman satış-öncesi doğru maliyet tabanına döndürmezdi.
        ///
        /// DÜZELTME: `WacService.ReverseWacOnSaleDeleteAsync` eklendi — `ReverseWacOnPurchaseDeleteAsync`'in
        /// simetriği: satış anında `CurrencyWacHistory`'ye loglanan gerçek WAC değerini okuyup, geri
        /// eklenen miktarın maliyetini o WAC ile toplam maliyet havuzuna ekleyip yeniden bölüyor.
        /// `RemoveTransaction` artık stale projeksiyon okuması yerine doğrudan bu metodu çağırıyor.
        /// </summary>
        [Fact]
        public async Task RemoveTransaction_SatisSilinince_BakiyeVeWacYanlisKalir_DenetimBulgusuKaniti()
        {
            // Aynı senaryo (alış->satış->alış->satışı geri al) 6 kez, her seferinde SIFIRDAN
            // (yeni ofis/kasa/para birimi) tekrarlanıyor. Sonuçlar farklı çalıştırmalar arasında
            // TUTARSIZ çıkıyorsa (bazen doğru 200, bazen yanlış bir değer), bu tek başına
            // RemoveTransaction'ın eşzamanlılık bile olmadan belirsiz (non-deterministic)
            // davrandığının kanıtıdır — ki bu, üç ayrı bağımsız çalıştırmada gözlemlenmiştir.
            const int iterations = 6;
            var observedBalances = new List<decimal>();
            var observedWacs = new List<decimal>();
            const decimal expectedBalance = 200m;
            const decimal expectedWacIfSaleNeverHappened = 50m;

            for (int i = 0; i < iterations; i++)
            {
                var s = await SeedAsync();
                await AddRateAsync(s.OfficeId, s.ForeignId, s.TryId, buy: 40m, sell: 42m);

                using (var ctx = TestDbContextFactory.Create())
                {
                    var service = CreateService(ctx, s.OwnerUserId);
                    await service.ProcessExchangeAsync(new List<rm_exchangetransaction>
                    {
                        new rm_exchangetransaction { VaultId = s.VaultId, SourceCurrencyId = s.ForeignId, TargetCurrencyId = s.TryId, SourceAmount = 100m, IsBuyingFromCustomer = true }
                    });
                }

                Guid saleTransactionId;
                using (var ctx = TestDbContextFactory.Create())
                {
                    var service = CreateService(ctx, s.OwnerUserId);
                    await service.ProcessExchangeAsync(new List<rm_exchangetransaction>
                    {
                        new rm_exchangetransaction { VaultId = s.VaultId, SourceCurrencyId = s.ForeignId, TargetCurrencyId = s.TryId, SourceAmount = 50m, IsBuyingFromCustomer = false }
                    });
                    saleTransactionId = (await ctx.Transactions.SingleAsync(t => t.VaultId == s.VaultId && t.Profit == 100m)).Id;
                }

                using (var ctx = TestDbContextFactory.Create())
                {
                    var service = CreateService(ctx, s.OwnerUserId);
                    await service.ProcessExchangeAsync(new List<rm_exchangetransaction>
                    {
                        new rm_exchangetransaction { VaultId = s.VaultId, SourceCurrencyId = s.ForeignId, TargetCurrencyId = s.TryId, SourceAmount = 100m, IsBuyingFromCustomer = true, CustomRate = 60m, OwnerOverrideLoss = true }
                    });
                }

                using (var ctx = TestDbContextFactory.Create())
                {
                    var service = CreateService(ctx, s.OwnerUserId);
                    await service.RemoveTransaction(new rm_removetransaction { transactionId = saleTransactionId, reason = "Denetim testi" });
                }

                // Tamamen bağımsız, taze bir ADO.NET bağlantısıyla (EF context/identity map'i devre
                // dışı) gerçek veritabanı durumunu okuyoruz — şüpheye yer bırakmaz.
                await using var verifyConn = new Microsoft.Data.SqlClient.SqlConnection(
                    @"Server=.\SQLEXPRESS;Database=BaskentEnerjiTests;Trusted_Connection=True;TrustServerCertificate=True;");
                await verifyConn.OpenAsync();

                decimal wacValue;
                await using (var cmd = verifyConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Wac FROM CurrencyWacs WHERE VaultId=@v AND CurrencyId=@c";
                    cmd.Parameters.AddWithValue("@v", s.VaultId);
                    cmd.Parameters.AddWithValue("@c", s.ForeignId);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    wacValue = reader.GetDecimal(0);
                }

                decimal balanceValue;
                await using (var cmd = verifyConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Balance FROM VaultBalances WHERE VaultId=@v AND CurrencyId=@c";
                    cmd.Parameters.AddWithValue("@v", s.VaultId);
                    cmd.Parameters.AddWithValue("@c", s.ForeignId);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    await reader.ReadAsync();
                    balanceValue = reader.GetDecimal(0);
                }

                observedBalances.Add(balanceValue);
                observedWacs.Add(wacValue);
            }

            // SONUÇ (6/6 çalıştırma, bağımsız ADO.NET bağlantısıyla doğrulandı) — DÜZELTME SONRASI:
            //
            // a) BAKİYE RESTORE'U — tutarlı ve doğru (6/6 çalıştırmada 200).
            //
            // b) WAC ALANI — düzeltme öncesi 6/6 çalıştırmada hatalı biçimde 53.33'te donuk
            //    kalıyordu (satıştan hemen önceki, harmanlanmış değer). `WacService.
            //    ReverseWacOnSaleDeleteAsync` eklenmesi ve `RemoveTransaction`'ın artık bu metodu
            //    çağırması sonrasında WAC, satış anında loglanan gerçek WAC (40) kullanılarak
            //    maliyet tabanına simetrik olarak geri ekleniyor ve doğru sonuca (50) ulaşıyor.
            Assert.All(observedBalances, b => Assert.Equal(expectedBalance, b));
            Assert.All(observedWacs, w => Assert.Equal(expectedWacIfSaleNeverHappened, w));
        }

        [Fact]
        public async Task GetOrCreateWacAsync_AyniParaBiriminIlkAlisiEszamanliOlursa_MukerrerSatirOlusmaz_DenetimBulgusuDuzeltmesi()
        {
            // DÜZELTME ÖNCESİ: WacService.GetOrCreateWacAsync, CurrencyWacs(VaultId, CurrencyId)
            // üzerinde unique kısıt olmadığından, bir para biriminin ilk alışı iki eşzamanlı
            // istekle yapıldığında UPDLOCK henüz var olmayan bir satırı koruyamıyor ve iki ayrı
            // satır oluşuyordu (6/6 çalıştırmada rowCount=2 olarak kanıtlandı).
            //
            // DÜZELTME: (VaultId, CurrencyId) üzerine DB-seviyesi unique index eklendi
            // (AddUniqueIndexOnCurrencyWacs migration'ı) ve GetOrCreateWacAsync artık ikinci
            // eşzamanlı INSERT'in unique-kısıt ihlaliyle reddedilmesini yakalayıp satırı tekrar
            // UPDLOCK'lu okuyarak mükerrer satır oluşmasını engelliyor.
            var s = await SeedAsync();

            using var ctx1 = TestDbContextFactory.Create();
            using var ctx2 = TestDbContextFactory.Create();
            var wac1 = new WacService(ctx1);
            var wac2 = new WacService(ctx2);

            await Task.WhenAll(
                wac1.RecalculateWacOnPurchaseAsync(s.VaultId, s.ForeignId, 100m, 40m, null),
                wac2.RecalculateWacOnPurchaseAsync(s.VaultId, s.ForeignId, 100m, 40m, null)
            );

            await using var verifyConn = new Microsoft.Data.SqlClient.SqlConnection(
                @"Server=.\SQLEXPRESS;Database=BaskentEnerjiTests;Trusted_Connection=True;TrustServerCertificate=True;");
            await verifyConn.OpenAsync();
            int rowCount;
            await using (var cmd = verifyConn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM CurrencyWacs WHERE VaultId=@v AND CurrencyId=@c";
                cmd.Parameters.AddWithValue("@v", s.VaultId);
                cmd.Parameters.AddWithValue("@c", s.ForeignId);
                rowCount = (int)await cmd.ExecuteScalarAsync();
            }

            // DÜZELTME SONRASI: unique index + retry mantığı sayesinde eşzamanlı iki ilk-alış
            // isteği artık TEK bir satırla sonuçlanıyor.
            Assert.Equal(1, rowCount);
        }
    }
}
