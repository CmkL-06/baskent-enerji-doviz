using SmileMedical.Entity.Modals.RequestModals.ExchangeService.Party;
using SmileMedical.Entity.Modals.ViewModals.ExchangeOffice.Party;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmileMedical.Business.Infrastructure.ExchangeOffice.Party
{
    public interface IGhostPartyService
    {
        // Account Management
        Task<vm_ghostpartyaccount> CreateGhostAccountAsync(rm_ghostpartyaccount request);
        Task<vm_ghostpartyaccount> GetGhostAccountAsync(Guid accountId);
        Task<List<vm_ghostpartyaccount>> GetGhostAccountsByPartyAsync(Guid partyId);
        Task<vm_ghostpartyaccount> GetGhostAccountByCurrencyAsync(Guid partyId, Guid currencyId);
        Task<vm_ghostpartyaccount> BlockAmountAsync(Guid accountId, decimal amount, string reason);
        Task<vm_ghostpartyaccount> UnblockAmountAsync(Guid accountId, decimal amount);
        Task<decimal> GetAvailableBalanceAsync(Guid accountId);

        // Entry Management
        Task<vm_ghostpartyentry> CreateGhostEntryAsync(rm_ghostpartyentry request);
        Task<List<vm_ghostpartyentry>> GetGhostEntriesAsync(Guid accountId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<vm_ghostpartyentry> GetGhostEntryAsync(Guid entryId);
        Task<vm_ghostpartyentry> ReverseGhostEntryAsync(Guid entryId, string reason);
        Task<bool> ReconcileGhostEntryAsync(Guid entryId, Guid userId);

        // Transaction Processing
        Task<vm_ghostpartyentry> ProcessGhostPaymentAsync(rm_ghostpartypayment request);
        Task<vm_ghostpartyentry> ProcessGhostCollectionAsync(rm_ghostpartycollection request);

        // Balance and Reporting
        Task<vm_ghostpartybalance> GetGhostBalanceSummaryAsync(Guid partyId);
        Task<List<vm_ghostpartystatement>> GetGhostStatementAsync(Guid partyId, DateTime fromDate, DateTime toDate);
        Task<vm_ghostpartysummary> GetGhostPartySummaryAsync(Guid partyId);

        // Vault Integration
        Task UpdateVaultForGhostEntryAsync(Guid vaultId, Guid currencyId, decimal amount, bool isDebit, Guid userId);
        Task<bool> ValidateGhostTransactionAsync(Guid partyId, Guid currencyId, decimal amount, bool isDebit);
    }
}