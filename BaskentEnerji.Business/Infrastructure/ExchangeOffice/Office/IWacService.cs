using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office
{
    public interface IWacService
    {
        Task<decimal> GetWacAsync(Guid vaultId, Guid currencyId);
        Task<Dictionary<Guid, decimal>> GetAllWacsForVaultAsync(Guid vaultId);
        Task RecalculateWacOnPurchaseAsync(Guid vaultId, Guid currencyId, decimal purchaseAmount, decimal purchaseRate, Guid? transactionId = null);
        Task<decimal> CalculateRealizedProfitAsync(decimal sellRate, decimal quantity, Guid vaultId, Guid currencyId);
        Task AdjustWacQuantityAsync(Guid vaultId, Guid currencyId, decimal newQuantity, WacAdjustReason reason, Guid? transactionId = null);
        Task ReverseWacOnPurchaseDeleteAsync(Guid vaultId, Guid currencyId, decimal originalAmount, decimal originalRate, Guid? transactionId = null);
        Task ReverseWacOnSaleDeleteAsync(Guid vaultId, Guid currencyId, decimal soldAmount, decimal wacAtSaleTime, Guid? transactionId = null);
        Task SeedInitialWacAsync(Guid vaultId, Guid currencyId, decimal quantity, decimal initialRate);
    }

    public enum WacAdjustReason
    {
        Sale,
        DayClosure,
        ManualAdjustment,
        TransactionDelete
    }
}
