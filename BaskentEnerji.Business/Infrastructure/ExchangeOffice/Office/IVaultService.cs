using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office
{
    public interface IVaultService
    {

        Task SaveVault(rm_savevault data);
        Task RemoveVault(Guid id);
        Task<List<vm_vaultsummary>> GetAllVaultSummariesAsync();
        Task<vm_vaultsummary> GetVaultSummaryAsync(Guid vaultId);
        Task<List<vm_officesummary>> GetOfficeSummariesAsync();
        Task<decimal> GetTotalAssetsInBaseCurrencyAsync(Guid? officeId = null);
        Task UpdateVaultBalanceAsync(rm_updatevaultbalance data);
        Task VoidVaultBalanceHistoryAsync(Guid historyId, string reason);
        Task VoidVaultBalanceHistoriesAsync(List<Guid> historyIds, string reason);
        Task<bool> CheckVaultBalanceAsync(Guid vaultId, Guid currencyId, decimal requiredAmount);
        Task<decimal> GetLockedBalanceAsync(Guid vaultId, Guid currencyId);
        
        // Balance History Methods
        Task<object> GetVaultBalanceHistoriesByOfficeAsync(Guid officeId, DateTime? date = null);
        Task<object> GetVaultBalanceHistoriesByVaultAsync(Guid vaultId, DateTime? startDate = null, DateTime? endDate = null, int limit = 100);
        
        // Cache Methods
        void ClearVaultCaches();
        
        // Vault Counting Methods
        Task<bool> SubmitVaultCountAsync(rm_vaultcount data);
        Task<List<vm_vaultcount>> GetVaultCountsAsync(Guid vaultId, DateTime? startDate = null, DateTime? endDate = null);
        Task<bool> ResetVaultCountStatusAsync(Guid vaultId);
        Task<bool> SetVaultShouldCountAsync(Guid vaultId, bool shouldCount);
    }
}
