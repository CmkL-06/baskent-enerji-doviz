using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaskentEnerji.Business.Services.ExchangeOffice.Office;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace BaskentEnerji.Business.Tests
{
    /// <summary>
    /// Z-Raporu hesaplama mantığı testleri — özellikle bu oturumda düzeltilen iki şey:
    /// (1) MARJ = Kâr / Satış Hasılatı (eski formül Kâr / Alış Maliyeti idi ve alım hacmi
    /// düşükken anlamsız yüzdeler veriyordu), (2) çoklu şube görünümünde WAC'ın ağırlıklı
    /// ortalamayla doğru toplanması (önceden hiç toplanmıyor, hep "—" gösteriyordu).
    /// ZReportService okuma-ağırlıklı bir raporlama servisi olduğundan gerçek bir gün içi
    /// finansal mutasyon riski yok — WacService/DayClosureService testlerinden farklı olarak
    /// veriler doğrudan seed edilir (WAC/bakiye akışını simüle etmeye gerek yok).
    /// </summary>
    [Collection("SharedTestDatabase")]
    public class ZReportServiceTests
    {
        private ZReportService CreateService(BaskentEnerjiDbContext ctx)
        {
            var wacService = new WacService(ctx);
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            // IVaultService ve ValidationService bu testlerin dokunduğu kod yollarında
            // (GenerateSingleOfficeReport / GenerateMultiOfficeReport) hiç kullanılmıyor.
            return new ZReportService(ctx, null!, wacService, null!, memoryCache);
        }

        private async Task<Guid> CreateCurrencyAsync(string code)
        {
            using var ctx = TestDbContextFactory.Create();

            // TRY, üretimde olduğu gibi tekil/global bir referans para birimi olmalı — paylaşılan test
            // veritabanında her test kendi "TRY" satırını oluşturursa, ZReportService'in CurrencyCode'a
            // göre kurduğu global sözlük "duplicate key" hatası verir. Var olanı yeniden kullan.
            if (code == "TRY")
            {
                var existing = await ctx.Currencies.FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
                if (existing != null)
                    return existing.Id;
            }

            var currency = new Currency { Id = Guid.NewGuid(), CurrencyCode = code + Guid.NewGuid().ToString("N").Substring(0, 4) };
            ctx.Currencies.Add(currency);
            await ctx.SaveChangesAsync();
            return currency.Id;
        }

        private async Task<(Guid officeId, Guid vaultId)> CreateOfficeAndVaultAsync(string suffix)
        {
            using var ctx = TestDbContextFactory.Create();
            var office = new Office { Id = Guid.NewGuid(), OfficeName = "ZReport Test Ofis " + suffix };
            var vault = new Vault { Id = Guid.NewGuid(), Name = "Test Kasa", Description = "", OfficeId = office.Id, Type = Vault.VaultType.Main };
            ctx.Offices.Add(office);
            ctx.Vaults.Add(vault);
            await ctx.SaveChangesAsync();
            return (office.Id, vault.Id);
        }

        private async Task AddExchangeTransactionAsync(Guid vaultId, DateTime date,
            Guid foreignCurrencyId, Guid tryCurrencyId, TransactionSide foreignSide, decimal foreignAmount, decimal rate, decimal profit)
        {
            using var ctx = TestDbContextFactory.Create();
            var user = new User { Id = Guid.NewGuid(), Username = "tester_" + Guid.NewGuid(), Password = "", Mail = "", Firstname = "T", Lastname = "U" };
            ctx.Users.Add(user);

            var trySide = foreignSide == TransactionSide.Credit ? TransactionSide.Debit : TransactionSide.Credit;
            var tryAmount = foreignAmount * rate;

            var tx = new Transaction
            {
                Id = Guid.NewGuid(),
                TransactionNumber = "TX-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                VaultId = vaultId,
                UserId = user.Id,
                Type = TransactionType.Exchange,
                TransactionDate = date,
                Status = TransactionStatus.Completed,
                Profit = profit,
                IsDeleted = false,
                Details = new List<TransactionDetail>
                {
                    new TransactionDetail { CurrencyId = foreignCurrencyId, Side = foreignSide, Amount = foreignAmount, Rate = rate },
                    new TransactionDetail { CurrencyId = tryCurrencyId, Side = trySide, Amount = tryAmount, Rate = 1m }
                }
            };

            ctx.Transactions.Add(tx);
            await ctx.SaveChangesAsync();
        }

        private async Task<Guid> CreateUserAsync(string suffix)
        {
            using var ctx = TestDbContextFactory.Create();
            var user = new User { Id = Guid.NewGuid(), Username = "emp_" + suffix + "_" + Guid.NewGuid().ToString("N").Substring(0, 6), Password = "", Mail = "", Firstname = "Emp", Lastname = suffix };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            return user.Id;
        }

        // AddExchangeTransactionAsync'in aynısı, ama kendi rastgele kullanıcısını oluşturmak yerine
        // verilen (bilinen) UserId'yi kullanır — personel bazlı kırılımı birden fazla işlemde aynı
        // personele atfetmek için gerekli.
        private async Task AddExchangeTransactionAsUserAsync(Guid vaultId, Guid userId, DateTime date,
            Guid foreignCurrencyId, Guid tryCurrencyId, TransactionSide foreignSide, decimal foreignAmount, decimal rate, decimal profit)
        {
            using var ctx = TestDbContextFactory.Create();

            var trySide = foreignSide == TransactionSide.Credit ? TransactionSide.Debit : TransactionSide.Credit;
            var tryAmount = foreignAmount * rate;

            var tx = new Transaction
            {
                Id = Guid.NewGuid(),
                TransactionNumber = "TX-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                VaultId = vaultId,
                UserId = userId,
                Type = TransactionType.Exchange,
                TransactionDate = date,
                Status = TransactionStatus.Completed,
                Profit = profit,
                IsDeleted = false,
                Details = new List<TransactionDetail>
                {
                    new TransactionDetail { CurrencyId = foreignCurrencyId, Side = foreignSide, Amount = foreignAmount, Rate = rate },
                    new TransactionDetail { CurrencyId = tryCurrencyId, Side = trySide, Amount = tryAmount, Rate = 1m }
                }
            };

            ctx.Transactions.Add(tx);
            await ctx.SaveChangesAsync();
        }

        [Fact]
        public async Task GetDailyZReport_IkiFarkliPersonel_DogruKirilimVeKarHesaplar()
        {
            var tryId = await CreateCurrencyAsync("TRY");
            var usdId = await CreateCurrencyAsync("TST");
            var (officeId, vaultId) = await CreateOfficeAndVaultAsync("EMP1");
            var alice = await CreateUserAsync("Alice");
            var bob = await CreateUserAsync("Bob");
            var today = DateTime.UtcNow.Date.AddHours(10);

            await AddExchangeTransactionAsUserAsync(vaultId, alice, today, usdId, tryId, TransactionSide.Credit, 100m, 40m, profit: 50m);
            await AddExchangeTransactionAsUserAsync(vaultId, alice, today, usdId, tryId, TransactionSide.Debit, 50m, 42m, profit: 30m);
            await AddExchangeTransactionAsUserAsync(vaultId, bob, today, usdId, tryId, TransactionSide.Credit, 20m, 41m, profit: 10m);

            using var reportCtx = TestDbContextFactory.Create();
            var service = CreateService(reportCtx);
            var report = await service.GetDailyZReport(officeId, today);

            Assert.Equal(2, report.EmployeeBreakdown.Count);

            var aliceRow = report.EmployeeBreakdown.Single(e => e.UserId == alice);
            Assert.Equal(2, aliceRow.TransactionCount);
            Assert.Equal(80m, aliceRow.TotalProfit); // 50 + 30
            Assert.Equal("Emp Alice", aliceRow.EmployeeName);

            var bobRow = report.EmployeeBreakdown.Single(e => e.UserId == bob);
            Assert.Equal(1, bobRow.TransactionCount);
            Assert.Equal(10m, bobRow.TotalProfit);
        }

        [Fact]
        public async Task GetDailyZReport_TumSubelerGorunumu_PersonelKirilimiBosDoner()
        {
            var tryId = await CreateCurrencyAsync("TRY");
            var usdId = await CreateCurrencyAsync("TST");
            var (officeId, vaultId) = await CreateOfficeAndVaultAsync("EMP2");
            var alice = await CreateUserAsync("AliceMulti");
            var today = DateTime.UtcNow.Date.AddHours(10);

            await AddExchangeTransactionAsUserAsync(vaultId, alice, today, usdId, tryId, TransactionSide.Credit, 10m, 40m, profit: 5m);

            using var reportCtx = TestDbContextFactory.Create();
            var service = CreateService(reportCtx);
            var report = await service.GetDailyZReport(null, today); // Tüm Şubeler

            Assert.NotNull(report.EmployeeBreakdown);
            Assert.Empty(report.EmployeeBreakdown);
        }

        [Fact]
        public async Task GetDailyZReport_AlisVeSatisIslemi_DogruKarVeMarjHesaplar()
        {
            var tryId = await CreateCurrencyAsync("TRY");
            var usdId = await CreateCurrencyAsync("TST");
            var (officeId, vaultId) = await CreateOfficeAndVaultAsync("A1");
            var today = DateTime.UtcNow.Date.AddHours(10);

            // Alış: 100 USD, kur 40 -> 4000 TRY ödendi. Alışta henüz kâr yok.
            await AddExchangeTransactionAsync(vaultId, today, usdId, tryId, TransactionSide.Credit, 100m, 40m, profit: 0m);
            // Satış: 50 USD, kur 42 -> 2100 TRY alındı. WAC(40) üzerinden gerçekleşen kâr: (42-40)*50=100.
            await AddExchangeTransactionAsync(vaultId, today, usdId, tryId, TransactionSide.Debit, 50m, 42m, profit: 100m);

            // Güncel piyasa kuru + WAC + kasa bakiyesi (anlık K/Z hesaplaması için).
            using (var ctx = TestDbContextFactory.Create())
            {
                ctx.ExchangeRates.Add(new ExchangeRate
                {
                    Id = Guid.NewGuid(), OfficeId = officeId, SourceCurrencyId = usdId, TargetCurrencyId = tryId,
                    BuyRate = 41m, SellRate = 43m, IsActive = true, EffectiveFrom = today.AddHours(-1)
                });
                ctx.CurrencyWacs.Add(new CurrencyWac { Id = Guid.NewGuid(), VaultId = vaultId, CurrencyId = usdId, Wac = 40m, Quantity = 50m, LastUpdated = today });
                ctx.VaultBalances.Add(new VaultBalance { Id = Guid.NewGuid(), VaultId = vaultId, CurrencyId = usdId, Balance = 50m, LastUpdated = today });
                await ctx.SaveChangesAsync();
            }

            using var reportCtx = TestDbContextFactory.Create();
            var service = CreateService(reportCtx);
            var report = await service.GetDailyZReport(officeId, today);

            var usdDetail = report.CurrencyDetails.Single(d => d.CurrencyCode != "TRY");

            Assert.Equal(100m, usdDetail.TotalBoughtAmount);
            Assert.Equal(4000m, usdDetail.TotalBuyCost);
            Assert.Equal(50m, usdDetail.TotalSoldAmount);
            Assert.Equal(2100m, usdDetail.TotalSellRevenue);
            Assert.Equal(50m, usdDetail.NetPosition);

            // Kâr, alış-satış hacmi farkından değil, işlemlerin gerçek Profit alanlarının toplamından gelir.
            Assert.Equal(100m, usdDetail.Profit);

            // MARJ = Kâr / Satış Hasılatı = 100 / 2100 * 100 (eski hatalı formül Kâr/AlışMaliyeti=100/4000 olurdu).
            Assert.Equal(Math.Round(100m / 2100m * 100m, 4), Math.Round(usdDetail.ProfitMargin, 4));

            // Anlık kasa K/Z: (piyasa satış kuru - WAC) * kasadaki bakiye = (43-40)*50 = 150.
            Assert.Equal(40m, usdDetail.Wac);
            Assert.Equal(50m, usdDetail.CurrentBalance);
            Assert.Equal(150m, usdDetail.UnrealizedProfit);
        }

        [Fact]
        public async Task GetDailyZReport_CokluSube_WacAgirlikliOrtalamaOlarakToplanir()
        {
            // Aynı para birimi iki farklı şubede (farklı WAC/bakiye ile) tutuluyor — "Tüm Şubeler"
            // görünümünde bu ikisinin WAC'ı basitçe toplanamaz, ağırlıklı ortalama alınmalı.
            var tryId = await CreateCurrencyAsync("TRY");
            var usdId = await CreateCurrencyAsync("TST");
            var (officeAId, vaultAId) = await CreateOfficeAndVaultAsync("B1");
            var (officeBId, vaultBId) = await CreateOfficeAndVaultAsync("B2");
            var today = DateTime.UtcNow.Date.AddHours(10);

            // Her iki şubede de bu para biriminin currencyDetailsMap'e girmesi için en az bir işlem
            // gerekiyor (WAC/bakiye alanları sadece o gün işlem görmüş para birimleri için dolduruluyor).
            await AddExchangeTransactionAsync(vaultAId, today, usdId, tryId, TransactionSide.Credit, 1m, 1m, profit: 0m);
            await AddExchangeTransactionAsync(vaultBId, today, usdId, tryId, TransactionSide.Credit, 1m, 1m, profit: 0m);

            using (var ctx = TestDbContextFactory.Create())
            {
                // Şube A: WAC=40, bakiye=100 -> maliyet tabanı 4000
                ctx.CurrencyWacs.Add(new CurrencyWac { Id = Guid.NewGuid(), VaultId = vaultAId, CurrencyId = usdId, Wac = 40m, Quantity = 100m, LastUpdated = today });
                ctx.VaultBalances.Add(new VaultBalance { Id = Guid.NewGuid(), VaultId = vaultAId, CurrencyId = usdId, Balance = 100m, LastUpdated = today });

                // Şube B: WAC=50, bakiye=50 -> maliyet tabanı 2500
                ctx.CurrencyWacs.Add(new CurrencyWac { Id = Guid.NewGuid(), VaultId = vaultBId, CurrencyId = usdId, Wac = 50m, Quantity = 50m, LastUpdated = today });
                ctx.VaultBalances.Add(new VaultBalance { Id = Guid.NewGuid(), VaultId = vaultBId, CurrencyId = usdId, Balance = 50m, LastUpdated = today });

                await ctx.SaveChangesAsync();
            }

            using var reportCtx = TestDbContextFactory.Create();
            var service = CreateService(reportCtx);
            // "Tüm Şubeler" görünümü: officeId = null. Bu görünüm PAYLAŞILAN test veritabanındaki
            // TÜM aktif ofisleri tarar (sadece bu testinkileri değil) — bu yüzden "!= TRY" yerine
            // bu testin ürettiği rastgele son ekli koda göre filtrelemek gerekiyor.
            var report = await service.GetDailyZReport(null, today);
            var usdCode = (await reportCtx.Currencies.FindAsync(usdId))!.CurrencyCode;

            var usdDetail = report.CurrencyDetails.Single(d => d.CurrencyCode == usdCode);

            // Toplam bakiye = 150, toplam maliyet tabanı = 4000+2500 = 6500 -> ağırlıklı ortalama WAC = 43.333...
            Assert.Equal(150m, usdDetail.CurrentBalance);
            Assert.Equal(Math.Round(6500m / 150m, 6), Math.Round(usdDetail.Wac, 6));
        }

        [Fact]
        public async Task GetDailyZReport_NormalIslem_TryKendiSatiriOlarakGorunmez_DenetimBulgusuKaniti()
        {
            // DENETİM BULGUSU: TRY, alınıp satılan bir "döviz" değildir — sadece karşı bacağın (yabancı
            // para biriminin) TRY karşılığıdır. Düzeltme öncesi kod, her işlemin TRY detayını da
            // currencyDetailsMap'e ekliyordu; bu da "Döviz Bazlı Özet" tablosunda USD/EUR yanında
            // anlamsız, devasa rakamlı bir "TRY" satırı olarak görünüyordu.
            var tryId = await CreateCurrencyAsync("TRY");
            var usdId = await CreateCurrencyAsync("TST");
            var (officeId, vaultId) = await CreateOfficeAndVaultAsync("C1");
            var today = DateTime.UtcNow.Date.AddHours(10);

            await AddExchangeTransactionAsync(vaultId, today, usdId, tryId, TransactionSide.Credit, 100m, 42m, profit: 0m);

            using var reportCtx = TestDbContextFactory.Create();
            var service = CreateService(reportCtx);
            var report = await service.GetDailyZReport(officeId, today);

            Assert.DoesNotContain(report.CurrencyDetails, d => d.CurrencyCode == "TRY");
        }

        [Fact]
        public async Task GetDailyZReport_TransferIslemi_HacimSayacinaKarismaz_DenetimBulgusuKaniti()
        {
            // DENETİM BULGUSU: Şubeler arası transfer gerçek bir müşteri alım-satımı değildir.
            // Düzeltme öncesi kod, TotalVolumesByCurrency'yi işlem tipi kontrolü OLMADAN güncelliyordu
            // — bu yüzden bir transfer de "ciro" (hacim) sayacına dahil oluyordu.
            var tryId = await CreateCurrencyAsync("TRY");
            var usdId = await CreateCurrencyAsync("TST");
            var (officeId, vaultId) = await CreateOfficeAndVaultAsync("C2");
            var today = DateTime.UtcNow.Date.AddHours(10);

            // Gerçek alım-satım: 100 USD.
            await AddExchangeTransactionAsync(vaultId, today, usdId, tryId, TransactionSide.Credit, 100m, 42m, profit: 0m);

            // Şubeler arası transfer: 500 USD (gerçek alım-satım DEĞİL — tek bacaklı, TRY karşılığı yok).
            using (var ctx = TestDbContextFactory.Create())
            {
                var user = new User { Id = Guid.NewGuid(), Username = "tester_" + Guid.NewGuid(), Password = "", Mail = "", Firstname = "T", Lastname = "U" };
                ctx.Users.Add(user);
                ctx.Transactions.Add(new Transaction
                {
                    Id = Guid.NewGuid(),
                    TransactionNumber = "TX-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    VaultId = vaultId,
                    UserId = user.Id,
                    Type = TransactionType.Transfer,
                    TransactionDate = today,
                    Status = TransactionStatus.Completed,
                    Profit = 0,
                    IsDeleted = false,
                    Details = new List<TransactionDetail>
                    {
                        new TransactionDetail { CurrencyId = usdId, Side = TransactionSide.Debit, Amount = 500m, Rate = 1m }
                    }
                });
                await ctx.SaveChangesAsync();
            }

            using var reportCtx = TestDbContextFactory.Create();
            var service = CreateService(reportCtx);
            var report = await service.GetDailyZReport(officeId, today);

            // Hacim sayacı sadece gerçek alım-satımdan gelen 100 USD'yi görmeli, transferin 500 USD'sini değil.
            Assert.Equal(100m, report.Summary.TotalVolumesByCurrency.GetValueOrDefault(
                report.CurrencyDetails.Single().CurrencyCode));
        }

        [Fact]
        public async Task GetDailyZReport_ArbitrajIslemi_KarTekParaBirimineYanlisAtanmaz_DenetimBulgusuKaniti()
        {
            // DENETİM BULGUSU: Arbitraj (çapraz kur) işlemlerinde İKİ yabancı bacak vardır (TRY yok).
            // Düzeltme öncesi kod, işlemin TÜM kârını rastgele "ilk bulunan" para birimine atıyordu.
            // Düzeltme sonrası bu kâr artık summary.TotalArbitrageProfit'te ayrıca toplanıyor.
            var eurId = await CreateCurrencyAsync("TST_EUR");
            var usdtId = await CreateCurrencyAsync("TST_USDT");
            var (officeId, vaultId) = await CreateOfficeAndVaultAsync("C3");
            var today = DateTime.UtcNow.Date.AddHours(10);

            using (var ctx = TestDbContextFactory.Create())
            {
                var user = new User { Id = Guid.NewGuid(), Username = "tester_" + Guid.NewGuid(), Password = "", Mail = "", Firstname = "T", Lastname = "U" };
                ctx.Users.Add(user);
                ctx.Transactions.Add(new Transaction
                {
                    Id = Guid.NewGuid(),
                    TransactionNumber = "TX-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    VaultId = vaultId,
                    UserId = user.Id,
                    Type = TransactionType.Exchange,
                    TransactionDate = today,
                    Status = TransactionStatus.Completed,
                    Profit = 250m,
                    IsDeleted = false,
                    Details = new List<TransactionDetail>
                    {
                        // EUR alındı (yabancı bacak 1), USDT verildi (yabancı bacak 2) — TRY hiç yok.
                        new TransactionDetail { CurrencyId = eurId, Side = TransactionSide.Credit, Amount = 100m, Rate = 45m },
                        new TransactionDetail { CurrencyId = usdtId, Side = TransactionSide.Debit, Amount = 110m, Rate = 41m }
                    }
                });
                await ctx.SaveChangesAsync();
            }

            using var reportCtx = TestDbContextFactory.Create();
            var service = CreateService(reportCtx);
            var report = await service.GetDailyZReport(officeId, today);

            Assert.Equal(250m, report.Summary.TotalArbitrageProfit);

            // Kâr, EUR veya USDT satırlarından hiçbirine 250 olarak (tüm işlem kârı) yanlış atanmamalı.
            var eurCode = (await reportCtx.Currencies.FindAsync(eurId))!.CurrencyCode;
            var usdtCode = (await reportCtx.Currencies.FindAsync(usdtId))!.CurrencyCode;
            var eurDetail = report.CurrencyDetails.Single(d => d.CurrencyCode == eurCode);
            var usdtDetail = report.CurrencyDetails.Single(d => d.CurrencyCode == usdtCode);
            Assert.NotEqual(250m, eurDetail.Profit);
            Assert.NotEqual(250m, usdtDetail.Profit);
        }
    }
}
