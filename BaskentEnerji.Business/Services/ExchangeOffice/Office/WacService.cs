using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Office
{
    public class WacService : IWacService
    {
        private readonly BaskentEnerjiDbContext _context;

        public WacService(BaskentEnerjiDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetWacAsync(Guid vaultId, Guid currencyId)
        {
            var wac = await _context.CurrencyWacs
                .FirstOrDefaultAsync(w => w.VaultId == vaultId && w.CurrencyId == currencyId);
            return wac?.Wac ?? 0;
        }

        public async Task<Dictionary<Guid, decimal>> GetAllWacsForVaultAsync(Guid vaultId)
        {
            return await _context.CurrencyWacs
                .Where(w => w.VaultId == vaultId && w.Quantity > 0)
                .ToDictionaryAsync(w => w.CurrencyId, w => w.Wac);
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
            var wac = await GetWacAsync(vaultId, currencyId);
            if (wac == 0) return 0;
            return (sellRate - wac) * quantity;
        }

        public async Task AdjustWacQuantityAsync(Guid vaultId, Guid currencyId, decimal newQuantity, WacAdjustReason reason, Guid? transactionId = null)
        {
            var wac = await GetOrCreateWacAsync(vaultId, currencyId);
            var oldQuantity = wac.Quantity;

            if (newQuantity < 0) newQuantity = 0;

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
                _ => WacChangeReason.Adjustment
            };

            await LogWacHistoryAsync(vaultId, currencyId, wac.Wac, wac.Wac, oldQuantity, newQuantity, Math.Abs(newQuantity - oldQuantity), 0, transactionId, changeReason);
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
                wac.Wac = 0;
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
            var wac = await _context.CurrencyWacs
                .FirstOrDefaultAsync(w => w.VaultId == vaultId && w.CurrencyId == currencyId);

            if (wac == null)
            {
                wac = new CurrencyWac
                {
                    VaultId = vaultId,
                    CurrencyId = currencyId,
                    Wac = 0,
                    Quantity = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.CurrencyWacs.Add(wac);
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
