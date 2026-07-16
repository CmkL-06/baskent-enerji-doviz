using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Party;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Party;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Party;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOffice.Party;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BaskentEnerji.Business.Exceptions;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Party
{
    public class PartyCreditService : IPartyCreditService
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly ILogger<PartyCreditService> _logger;
        private readonly ValidationService _validationService;

        public PartyCreditService(BaskentEnerjiDbContext context, ILogger<PartyCreditService> logger, ValidationService validationService)
        {
            _context = context;
            _logger = logger;
            _validationService = validationService;
        }

        public async Task<vm_partycreditlimit> SetCreditLimitAsync(rm_partycreditlimit request)
        {
            var partyOfficeId = await _context.Parties
                .Where(p => p.Id == request.PartyId)
                .Select(p => p.OfficeId)
                .FirstOrDefaultAsync();
            if (partyOfficeId == default)
                throw new ApiException(HttpStatusCode.NotFound, "Party not found.");
            await _validationService.EnsureNotViewerAsync(partyOfficeId);

            var creditLimit = new PartyCreditLimit
            {
                PartyId = request.PartyId,
                CurrencyId = request.CurrencyId,
                CreditLimit = request.CreditLimit,
                PaymentTermDays = request.PaymentTermDays,
                InterestRate = request.InterestRate ?? 0,
                Notes = request.Notes,
                EffectiveFrom = DateTime.UtcNow,
                LastReviewDate = DateTime.UtcNow,
                ApprovedByUserId = request.ApprovedByUserId
            };

            _context.PartyCreditLimits.Add(creditLimit);
            await _context.SaveChangesAsync();

            return await GetActiveCreditLimitAsync(request.PartyId, request.CurrencyId);
        }

        public async Task<List<vm_partycreditlimit>> GetPartyCreditLimitsAsync(Guid partyId)
        {
            var limits = await _context.PartyCreditLimits
                .Include(cl => cl.Currency)
                .Include(cl => cl.ApprovedByUser)
                .Where(cl => cl.PartyId == partyId && cl.IsActive)
                .ToListAsync();

            return limits.Select(MapToViewModel).ToList();
        }

        public async Task<vm_creditavailability> CheckCreditAvailabilityAsync(Guid partyId, Guid currencyId, decimal requestedAmount)
        {
            var creditLimit = await _context.PartyCreditLimits
                .FirstOrDefaultAsync(cl => cl.PartyId == partyId && cl.CurrencyId == currencyId && cl.IsActive);

            var account = await _context.PartyAccounts
                .FirstOrDefaultAsync(a => a.PartyId == partyId && a.CurrencyId == currencyId);

            var currentBalance = account?.Balance ?? 0;
            var creditLimitAmount = creditLimit?.CreditLimit ?? 0;
            var available = creditLimitAmount - currentBalance;

            return new vm_creditavailability
            {
                PartyId = partyId,
                CurrencyId = currencyId,
                CreditLimit = creditLimitAmount,
                CurrentBalance = currentBalance,
                AvailableCredit = available,
                RequestedAmount = requestedAmount,
                IsApproved = available >= requestedAmount,
                HasCreditLimit = creditLimit != null
            };
        }

        public async Task<vm_partycreditlimit> GetActiveCreditLimitAsync(Guid partyId, Guid currencyId)
        {
            var limit = await _context.PartyCreditLimits
                .Include(cl => cl.Currency)
                .Include(cl => cl.ApprovedByUser)
                .FirstOrDefaultAsync(cl => cl.PartyId == partyId && cl.CurrencyId == currencyId && cl.IsActive);

            return limit != null ? MapToViewModel(limit) : null;
        }

        private vm_partycreditlimit MapToViewModel(PartyCreditLimit limit)
        {
            return new vm_partycreditlimit
            {
                Id = limit.Id,
                PartyId = limit.PartyId,
                CurrencyId = limit.CurrencyId,
                CurrencyCode = limit.Currency?.CurrencyCode,
                CreditLimit = limit.CreditLimit,
                UtilizedAmount = limit.UtilizedAmount,
                PaymentTermDays = limit.PaymentTermDays,
                InterestRate = limit.InterestRate,
                EffectiveFrom = limit.EffectiveFrom,
                EffectiveTo = limit.EffectiveTo,
                IsActive = limit.IsActive,
                ApprovedByUserName = limit.ApprovedByUser?.Username,
                LastReviewDate = limit.LastReviewDate
            };
        }

        // Other interface methods would be implemented here...
        public Task<vm_partycreditlimit> UpdateCreditLimitAsync(Guid creditLimitId, rm_partycreditlimit request)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeactivateCreditLimitAsync(Guid creditLimitId, DateTime effectiveTo)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ReserveCreditAsync(Guid partyId, Guid currencyId, decimal amount, string referenceNumber)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ReleaseCreditAsync(Guid partyId, Guid currencyId, decimal amount, string referenceNumber)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ConsumeCreditAsync(Guid partyId, Guid currencyId, decimal amount, string referenceNumber)
        {
            throw new NotImplementedException();
        }

        public Task<vm_creditutilization> GetCreditUtilizationAsync(Guid partyId)
        {
            throw new NotImplementedException();
        }

        public Task<List<vm_creditexposure>> GetCreditExposureByOfficeAsync(Guid officeId)
        {
            throw new NotImplementedException();
        }

        public Task<vm_credithistory> GetCreditHistoryAsync(Guid partyId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            throw new NotImplementedException();
        }

        public Task<decimal> CalculateInterestAsync(Guid partyId, Guid currencyId, DateTime fromDate, DateTime toDate)
        {
            throw new NotImplementedException();
        }

        public Task<vm_intereststatement> GenerateInterestStatementAsync(Guid partyId, DateTime fromDate, DateTime toDate)
        {
            throw new NotImplementedException();
        }
    }
}