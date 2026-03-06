using SmileMedical.Entity.Entities.ExchangeOffice.Party;
using SmileMedical.Entity.Modals.RequestModals.ExchangeService.Party;
using SmileMedical.Entity.Modals.ViewModals.ExchangeOffice.Party;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmileMedical.Business.Infrastructure.ExchangeOffice.Party
{
    public interface IPartyCreditService
    {
        // Credit Limit Management
        Task<vm_partycreditlimit> SetCreditLimitAsync(rm_partycreditlimit request);
        Task<vm_partycreditlimit> UpdateCreditLimitAsync(Guid creditLimitId, rm_partycreditlimit request);
        Task<vm_partycreditlimit> GetActiveCreditLimitAsync(Guid partyId, Guid currencyId);
        Task<List<vm_partycreditlimit>> GetPartyCreditLimitsAsync(Guid partyId);
        Task<bool> DeactivateCreditLimitAsync(Guid creditLimitId, DateTime effectiveTo);
        
        // Credit Validation
        Task<vm_creditavailability> CheckCreditAvailabilityAsync(Guid partyId, Guid currencyId, decimal requestedAmount);
        Task<bool> ReserveCreditAsync(Guid partyId, Guid currencyId, decimal amount, string referenceNumber);
        Task<bool> ReleaseCreditAsync(Guid partyId, Guid currencyId, decimal amount, string referenceNumber);
        Task<bool> ConsumeCreditAsync(Guid partyId, Guid currencyId, decimal amount, string referenceNumber);
        
        // Credit Analysis
        Task<vm_creditutilization> GetCreditUtilizationAsync(Guid partyId);
        Task<List<vm_creditexposure>> GetCreditExposureByOfficeAsync(Guid officeId);
        Task<vm_credithistory> GetCreditHistoryAsync(Guid partyId, DateTime? fromDate = null, DateTime? toDate = null);
        
        // Interest Calculation
        Task<decimal> CalculateInterestAsync(Guid partyId, Guid currencyId, DateTime fromDate, DateTime toDate);
        Task<vm_intereststatement> GenerateInterestStatementAsync(Guid partyId, DateTime fromDate, DateTime toDate);
    }
}