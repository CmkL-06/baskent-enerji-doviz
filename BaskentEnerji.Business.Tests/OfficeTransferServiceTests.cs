using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    /// OfficeTransferService.CreateTransferRequestAsync — denetim raporunda iddia edilen "günlük/aylık
    /// limit kontrolü UPDLOCK'suz, eşzamanlı iki transfer limiti birlikte aşabilir" bulgusunun ampirik
    /// kanıtı. Kaynak bakiye kontrolü UPDLOCK ile korunuyor (satır 51-53) ama hemen altındaki
    /// DailyTransactionLimit SumAsync sorgusu (satır 63-68) korunmuyor.
    /// </summary>
    [Collection("SharedTestDatabase")]
    public class OfficeTransferServiceTests
    {
        private class TestScenario
        {
            public Guid SourceVaultId;
            public Guid TargetVaultId;
            public Guid CurrencyId;
            public Guid OwnerUserId;
        }

        private async Task<TestScenario> SeedAsync(decimal dailyLimit)
        {
            using var ctx = TestDbContextFactory.Create();

            var office = new Office
            {
                Id = Guid.NewGuid(),
                OfficeName = "Transfer Test Ofis " + Guid.NewGuid(),
                DailyTransactionLimit = dailyLimit
            };
            var sourceVault = new Vault { Id = Guid.NewGuid(), Name = "Kaynak Kasa", Description = "", OfficeId = office.Id, Type = Vault.VaultType.Main };
            var targetVault = new Vault { Id = Guid.NewGuid(), Name = "Hedef Kasa", Description = "", OfficeId = office.Id, Type = Vault.VaultType.Main };
            var currency = new Currency { Id = Guid.NewGuid(), CurrencyCode = "TST" + Guid.NewGuid().ToString("N").Substring(0, 4) };
            var owner = new User { Id = Guid.NewGuid(), Username = "owner_" + Guid.NewGuid(), Password = "", Mail = "", Firstname = "Test", Lastname = "Owner", Rank = Rank.Owner };

            ctx.Offices.Add(office);
            ctx.Vaults.Add(sourceVault);
            ctx.Vaults.Add(targetVault);
            ctx.Currencies.Add(currency);
            ctx.Users.Add(owner);
            ctx.VaultBalances.Add(new VaultBalance { Id = Guid.NewGuid(), VaultId = sourceVault.Id, CurrencyId = currency.Id, Balance = 100000m });
            await ctx.SaveChangesAsync();

            return new TestScenario
            {
                SourceVaultId = sourceVault.Id,
                TargetVaultId = targetVault.Id,
                CurrencyId = currency.Id,
                OwnerUserId = owner.Id
            };
        }

        [Fact]
        public async Task CreateTransferRequestAsync_EszamanliIkiTransfer_GunlukLimitiBirlikteAsabilir_DenetimBulgusuKaniti()
        {
            // Günlük limit = 100. İki eşzamanlı transfer, her biri TEK BAŞINA limitin altında (60), ama
            // toplamda (120) limiti aşıyor. UPDLOCK'suz SumAsync nedeniyle her iki istek de "bugünkü
            // toplam 0" görüp ikisi de geçebilir.
            var s = await SeedAsync(dailyLimit: 100m);

            using var ctx1 = TestDbContextFactory.Create();
            using var ctx2 = TestDbContextFactory.Create();
            var validation1 = TestAuthHelper.CreateValidationService(s.OwnerUserId, ctx1);
            var validation2 = TestAuthHelper.CreateValidationService(s.OwnerUserId, ctx2);
            var service1 = new OfficeTransferService(ctx1, validation1);
            var service2 = new OfficeTransferService(ctx2, validation2);

            var request1 = new rm_create_officetransfer { SourceVaultId = s.SourceVaultId, TargetVaultId = s.TargetVaultId, CurrencyId = s.CurrencyId, Amount = 60m };
            var request2 = new rm_create_officetransfer { SourceVaultId = s.SourceVaultId, TargetVaultId = s.TargetVaultId, CurrencyId = s.CurrencyId, Amount = 60m };

            var results = await Task.WhenAll(
                SafeCreateAsync(service1, request1, s.OwnerUserId),
                SafeCreateAsync(service2, request2, s.OwnerUserId)
            );

            int successCount = 0;
            var errors = new List<string>();
            foreach (var r in results)
            {
                if (r.Item1) successCount++;
                else errors.Add(r.Item2!);
            }

            await using var verifyConn = new Microsoft.Data.SqlClient.SqlConnection(
                @"Server=.\SQLEXPRESS;Database=BaskentEnerjiTests;Trusted_Connection=True;TrustServerCertificate=True;");
            await verifyConn.OpenAsync();
            decimal completedTotal;
            await using (var cmd = verifyConn.CreateCommand())
            {
                cmd.CommandText = "SELECT ISNULL(SUM(Amount),0) FROM OfficeTransfers WHERE SourceVaultId=@v AND CurrencyId=@c AND Status=3"; // Completed
                cmd.Parameters.AddWithValue("@v", s.SourceVaultId);
                cmd.Parameters.AddWithValue("@c", s.CurrencyId);
                completedTotal = (decimal)await cmd.ExecuteScalarAsync();
            }

            // SONUÇ (4/4 çalıştırmada tutarlı): DENETİM RAPORUNDAKİ HİPOTEZ BU SENARYODA DOĞRULANAMADI.
            // Beklenti, SumAsync'in UPDLOCK'suz olması nedeniyle her iki eşzamanlı transferin de günlük
            // limiti görmeden geçebileceğiydi. Ancak GERÇEKTE, CreateTransferRequestAsync'in TAMAMI tek
            // bir `_db.Database.BeginTransactionAsync()` içinde çalışıyor ve az önceki UPDLOCK'lu bakiye
            // okuması (satır 51-53) aynı kaynak kasa+para birimi satırını commit'e kadar kilitli tutuyor.
            // Bu yüzden ikinci istek, kendi UPDLOCK okumasında BLOKE OLUYOR ve birinci transfer commit
            // olup satırı serbest bırakana kadar bekliyor — o noktada SumAsync artık birinci transferi
            // GÖREREK doğru şekilde reddediyor (successCount=1, ikincisi "Günlük işlem limiti aşıldı"
            // hatasıyla engelleniyor). Yani UPDLOCK'lu bakiye kontrolü, TESADÜFEN, SumAsync'i de dolaylı
            // olarak koruyor, çünkü ikisi aynı transaction içinde ardışık çalışıyor. Bu spesifik
            // senaryo (aynı kasa+para birimine eşzamanlı iki transfer) için race condition İDDİASI
            // GERİ ÇEKİLMİŞTİR. (Not: Bu, günlük limitin OfficeId değil SourceVaultId bazında
            // uygulandığını da doğruladı — farklı kasalar arası bir yarış senaryosu ayrı bir konudur,
            // bu testin kapsamında değildir.)
            Assert.Equal(1, successCount);
            Assert.Equal(60m, completedTotal);
        }

        private static async Task<(bool, string?)> SafeCreateAsync(OfficeTransferService service, rm_create_officetransfer request, Guid userId)
        {
            try
            {
                await service.CreateTransferRequestAsync(request, userId);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
