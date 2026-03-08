using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Party;
using MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Party;
using MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOffice.Party;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Infrastructure.ExchangeOffice.Party
{
    public interface IPartyAccountService
    {
        // Account Management
        Task<vm_partyaccount> CreateAccountAsync(Guid partyId, Guid currencyId);
        Task<vm_partyaccount> GetAccountAsync(Guid accountId);
        Task<vm_partyaccount> GetAccountByPartyAndCurrencyAsync(Guid partyId, Guid currencyId);
        Task<List<vm_partyaccount>> GetPartyAccountsAsync(Guid partyId);
        Task<bool> BlockAmountAsync(Guid accountId, decimal amount, string reason);
        Task<bool> UnblockAmountAsync(Guid accountId, decimal amount);
        Task<bool> UpdateAccountStatusAsync(Guid accountId, AccountStatus status, string reason);
        
        // Balance Inquiries
        Task<decimal> GetBalanceAsync(Guid partyId, Guid currencyId);
        Task<vm_partybalance> GetBalanceSummaryAsync(Guid partyId);
        Task<List<vm_partybalance>> GetAllBalancesAsync(Guid officeId, DateTime? asOfDate = null);
        
        // Account Entry Management
        Task<vm_partyaccountentry> CreateEntryAsync(rm_partyaccountentry request);
        Task<vm_partyaccountentry> CreateManualEntryAsync(rm_partyaccountentry request);
        Task<vm_partyaccountentry> ReverseEntryAsync(Guid entryId, string reason);
        Task<List<vm_partyaccountentry>> GetAccountEntriesAsync(Guid accountId, DateTime? fromDate = null, DateTime? toDate = null, PaymentStatus? status = null);
        Task<vm_partyaccountentry> GetEntryByReferenceAsync(string referenceNumber);
        Task<vm_partyaccountentry> RecordPaymentAsync(rm_partypayment request);
        
        // Reconciliation
        Task<bool> ReconcileEntryAsync(Guid entryId, Guid userId);
        Task<bool> UnreconcileEntryAsync(Guid entryId, string reason);
        Task<List<vm_partyaccountentry>> GetUnreconciledEntriesAsync(Guid accountId);
        
        // Payment Management
        Task<bool> UpdatePaymentStatusAsync(Guid entryId, PaymentStatus status, string paymentReference = null);
        Task<List<vm_partyaccountentry>> GetOverdueEntriesAsync(Guid? partyId = null, Guid? officeId = null);
    }
}