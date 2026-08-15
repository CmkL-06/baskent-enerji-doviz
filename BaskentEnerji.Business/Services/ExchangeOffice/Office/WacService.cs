using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Office
{
    public class WacService : IWacService
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly ILogger<WacService>? _logger;

        public WacService(BaskentEnerjiDbContext context, ILogger<WacService>? logger = null)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<decimal> GetWacAsync(Guid vaultId, Guid currencyId)
        {
            var wac = await _context.CurrencyWacs
                .FirstOrDefaultAsync(w => w.VaultId == vaultId && w.CurrencyId == currencyId);
            return wac?.Wac ?? 0;
        }

        public async Task<Dictionary<Guid, decimal>> GetAllWacsForVaultAsync(Guid vaultId)
        {
            // GroupBy + tekilleştirme: (VaultId, CurrencyId) üzerinde unique kısıt olmadığından
            // (bkz. GetOrCreateWacAsync'teki ilk-alış race condition), aynı çift için birden fazla
            // satır oluşmuş olabilir. ToDictionaryAsync bu durumda "duplicate key" hatasıyla çöker —
            // bu yüzden her para birimi için satırları önce grupluyoruz.
            var rows = await _context.CurrencyWacs
                .Where(w => w.VaultId == vaultId && w.Quantity > 0)
                .ToListAsync();

            return rows
                .GroupBy(w => w.CurrencyId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(w => w.LastUpdated).First().Wac);
        }

        public async Task RecalculateWacOnPurchaseAsync(Guid vaultId, Guid currencyId, decimal purchaseAmount, decimal purchaseRate, Guid? transactionId = null)
        {
            if (purchaseAmount <= 0) return;

            var wac = await GetOrCreateWacAsync(vaultId, currencyId);
            var oldWac = wac.Wac;
            var oldQuantity = wac.Quantity;

            var totalCost = (oldQuantity * oldWac) + (purchaseAmount * purchaseRate);
            var totalQuantity = oldQuantity + purchaseAmount;

            wac.Wac = totalQuantity > 0 ? totalCost / totalQuantity : 0;
            wac.Quantity = totalQuantity;
            wac.LastUpdated = DateTime.UtcNow;

            await LogWacHistoryAsync(vaultId, currencyId, oldWac, wac.Wac, oldQuantity, wac.Quantity, purchaseAmount, purchaseRate, transactionId, WacChangeReason.Purchase);
            await _context.SaveChangesAsync();
        }

        public async Task<decimal> CalculateRealizedProfitAsync(decimal sellRate, decimal quantity, Guid vaultId, Guid currencyId)
        {
            // Use locked read when inside a transaction to prevent stale WAC
            var wacEntity = await GetOrCreateWacAsync(vaultId, currencyId);
            if (wacEntity.Wac == 0)
            {
                // Denetim bulgusu: maliyet tabanı bilinmiyorsa (hiç alış yapılmamış/reversal ile
                // sıfırlanmış) kâr sessizce 0 gösteriliyordu ve satış yine de gerçekleşiyordu —
                // personel bunun farkına varamıyordu. Artık en azından log'a düşüyor; Z-Raporunda
                // beklenmedik düşük kâr görülürse bu logdan kök nedene ulaşılabilir.
                _logger?.LogWarning(
                    "Gerçekleşen kâr hesaplanamadı — WAC=0 (maliyet tabanı bilinmiyor), kâr 0 olarak kaydedildi. VaultId={VaultId}, CurrencyId={CurrencyId}, SatışKuru={SellRate}, Miktar={Quantity}",
                    vaultId, currencyId, sellRate, quantity);
                return 0;
            }
            return (sellRate - wacEntity.Wac) * quantity;
        }

        public async Task AdjustWacQuantityAsync(Guid vaultId, Guid currencyId, decimal newQuantity, WacAdjustReason reason, Guid? transactionId = null)
        {
            var wac = await GetOrCreateWacAsync(vaultId, currencyId);
            var oldQuantity = wac.Quantity;
            // Miktar tam sıfıra düştüğünde wac.Wac aşağıda 0'a çekiliyor — denetim kaydına
            // (LogWacHistoryAsync) o satırdan SONRAKİ (mutasyona uğramış) değer değil, satış
            // anındaki GERÇEK WAC yazılmalı. Aksi halde OldWac=0 olarak loglanır ve bu işlem
            // silinip geri alınmak istendiğinde yanlış (sıfır) maliyet tabanıyla geri eklenir.
            var wacBeforeAdjustment = wac.Wac;

            if (newQuantity < 0)
            {
                // Denetim raporu sertleştirmesi: negatif miktar sessizce sıfıra çekiliyordu — bu,
                // gerçek bir aşırı-satış (kasada olduğundan fazla satış yapılmış) durumunu fark
                // edilmeden gizleyebilir. Artık uyarı olarak loglanıyor.
                _logger?.LogWarning(
                    "WAC miktarı negatif hesaplandı ve sıfıra çekildi — olası aşırı satış. VaultId={VaultId}, CurrencyId={CurrencyId}, HesaplananMiktar={NewQuantity}, ÖncekiMiktar={OldQuantity}, TransactionId={TransactionId}, Reason={Reason}",
                    vaultId, currencyId, newQuantity, oldQuantity, transactionId, reason);
                newQuantity = 0;
            }

            wac.Quantity = newQuantity;
            wac.LastUpdated = DateTime.UtcNow;

            if (newQuantity == 0)
                wac.Wac = 0;

            var changeReason = reason switch
            {
                WacAdjustReason.Sale => WacChangeReason.Adjustment,
                WacAdjustReason.DayClosure => WacChangeReason.DayOpening,
                WacAdjustReason.ManualAdjustment => WacChangeReason.Adjustment,
                WacAdjustReason.TransactionDelete => WacChangeReason.Adjustment,
                WacAdjustReason.Transfer => WacChangeReason.Transfer,
                _ => WacChangeReason.Adjustment
            };

            await LogWacHistoryAsync(vaultId, currencyId, wacBeforeAdjustment, wac.Wac, oldQuantity, newQuantity, Math.Abs(newQuantity - oldQuantity), 0, transactionId, changeReason);
            await _context.SaveChangesAsync();
        }

        public async Task ReverseWacOnPurchaseDeleteAsync(Guid vaultId, Guid currencyId, decimal originalAmount, decimal originalRate, Guid? transactionId = null)
        {
            var wac = await GetOrCreateWacAsync(vaultId, currencyId);
            var oldWac = wac.Wac;
            var oldQuantity = wac.Quantity;

            var newQuantity = oldQuantity - originalAmount;
            if (newQuantity <= 0)
            {
                // Denetim bulgusu: bu alıştan sonra satış yapılmış ve/veya başka alışlarla
                // karışmış olabilir — miktar sıfırın altına düşüyor demek, kasada hâlâ BAŞKA
                // alışlardan gelen bakiye olabileceği anlamına gelir. WAC lot bazlı takip
                // yapmadığından bu alışın payını net olarak geri çıkaramıyoruz; miktarı VE
                // maliyeti (Wac) sıfırlamak, hâlâ kasada duran diğer alışların maliyet tabanını
                // da silerdi. Bunun yerine miktar sıfıra kilitlenir ama mevcut WAC korunur —
                // bir sonraki alışta zaten qty=0 olduğundan doğru şekilde yeniden hesaplanır,
                // araya bir satış girerse de CalculateRealizedProfitAsync sessizce 0 kâr
                // göstermek yerine son bilinen (daha doğru) maliyeti kullanır.
                _logger?.LogWarning(
                    "Alış iptali miktarı sıfırın altına düşürdü — muhtemelen araya satış girmiş. VaultId={VaultId}, CurrencyId={CurrencyId}, MevcutMiktar={OldQuantity}, İptalEdilenMiktar={OriginalAmount}, TransactionId={TransactionId}. WAC korunarak miktar sıfırlandı, manuel kasa sayımı kontrolü önerilir.",
                    vaultId, currencyId, oldQuantity, originalAmount, transactionId);
                wac.Quantity = 0;
            }
            else
            {
                var totalCostBefore = oldQuantity * oldWac;
                var removedCost = originalAmount * originalRate;
                var remainingCost = totalCostBefore - removedCost;
                wac.Wac = remainingCost > 0 ? remainingCost / newQuantity : oldWac;
                wac.Quantity = newQuantity;
            }
            wac.LastUpdated = DateTime.UtcNow;

            await LogWacHistoryAsync(vaultId, currencyId, oldWac, wac.Wac, oldQuantity, wac.Quantity, originalAmount, originalRate, transactionId, WacChangeReason.Adjustment);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Bir SATIŞ işlemi silindiğinde çağrılır (bkz. denetim raporu bulgusu — RemoveTransaction WAC
        /// hatası). Bir satış, miktarı azaltır ama maliyet tabanını (WAC) değiştirmez; bu yüzden satışı
        /// geri almak da simetrik olarak sadece miktarı geri eklemeli VE geri eklenen birimlerin
        /// maliyetini satış ANINDAKİ WAC ile (şu anki, muhtemelen sonraki alışlarla değişmiş WAC ile
        /// DEĞİL) toplam maliyet havuzuna eklemelidir — <see cref="ReverseWacOnPurchaseDeleteAsync"/>'in
        /// tam simetriği (o metot maliyeti ÇIKARIR, bu metot geri EKLER).
        /// </summary>
        public async Task ReverseWacOnSaleDeleteAsync(Guid vaultId, Guid currencyId, decimal soldAmount, decimal wacAtSaleTime, Guid? transactionId = null)
        {
            var wac = await GetOrCreateWacAsync(vaultId, currencyId);
            var oldWac = wac.Wac;
            var oldQuantity = wac.Quantity;

            var newQuantity = oldQuantity + soldAmount;
            var totalCostBefore = oldQuantity * oldWac;
            var restoredCost = soldAmount * wacAtSaleTime;
            var newTotalCost = totalCostBefore + restoredCost;

            wac.Wac = newQuantity > 0 ? newTotalCost / newQuantity : 0;
            wac.Quantity = newQuantity;
            wac.LastUpdated = DateTime.UtcNow;

            await LogWacHistoryAsync(vaultId, currencyId, oldWac, wac.Wac, oldQuantity, wac.Quantity, soldAmount, wacAtSaleTime, transactionId, WacChangeReason.Adjustment);
            await _context.SaveChangesAsync();
        }

        public async Task SeedInitialWacAsync(Guid vaultId, Guid currencyId, decimal quantity, decimal initialRate)
        {
            var existing = await _context.CurrencyWacs
                .FirstOrDefaultAsync(w => w.VaultId == vaultId && w.CurrencyId == currencyId);

            if (existing != null) return;

            var wac = new CurrencyWac
            {
                VaultId = vaultId,
                CurrencyId = currencyId,
                Wac = initialRate,
                Quantity = quantity,
                LastUpdated = DateTime.UtcNow
            };
            _context.CurrencyWacs.Add(wac);

            await LogWacHistoryAsync(vaultId, currencyId, 0, initialRate, 0, quantity, quantity, initialRate, null, WacChangeReason.InitialSeed);
            await _context.SaveChangesAsync();
        }

        private async Task<CurrencyWac> GetOrCreateWacAsync(Guid vaultId, Guid currencyId)
        {
            // Use UPDLOCK to prevent concurrent WAC updates from racing
            var wac = await _context.CurrencyWacs
                .FromSqlRaw("SELECT * FROM CurrencyWacs WITH (UPDLOCK) WHERE VaultId = {0} AND CurrencyId = {1}", vaultId, currencyId)
                .FirstOrDefaultAsync();

            if (wac != null)
                return wac;

            wac = new CurrencyWac
            {
                VaultId = vaultId,
                CurrencyId = currencyId,
                Wac = 0,
                Quantity = 0,
                LastUpdated = DateTime.UtcNow
            };
            _context.CurrencyWacs.Add(wac);

            try
            {
                // Denetim raporu düzeltmesi: UPDLOCK henüz var olmayan bir satırı koruyamadığından,
                // iki eşzamanlı ilk-alış isteği aynı anda "satır yok" görüp buraya gelebilir.
                // (VaultId, CurrencyId) üzerindeki DB-seviyesi unique index sayesinde ikinci INSERT
                // burada reddedilir; bu durumda eklenen entity'yi bırakıp satırı (artık var olduğu
                // için) tekrar UPDLOCK'lu okuyoruz — mükerrer satır oluşması engellenmiş olur.
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _context.Entry(wac).State = EntityState.Detached;
                wac = await _context.CurrencyWacs
                    .FromSqlRaw("SELECT * FROM CurrencyWacs WITH (UPDLOCK) WHERE VaultId = {0} AND CurrencyId = {1}", vaultId, currencyId)
                    .FirstOrDefaultAsync();
                if (wac == null)
                    throw;
            }

            return wac;
        }

        private async Task LogWacHistoryAsync(Guid vaultId, Guid currencyId, decimal oldWac, decimal newWac, decimal oldQuantity, decimal newQuantity, decimal txAmount, decimal txRate, Guid? transactionId, WacChangeReason reason)
        {
            _context.CurrencyWacHistories.Add(new CurrencyWacHistory
            {
                VaultId = vaultId,
                CurrencyId = currencyId,
                OldWac = oldWac,
                NewWac = newWac,
                OldQuantity = oldQuantity,
                NewQuantity = newQuantity,
                TransactionAmount = txAmount,
                TransactionRate = txRate,
                TransactionId = transactionId,
                Reason = reason
            });
            await Task.CompletedTask;
        }
    }
}
