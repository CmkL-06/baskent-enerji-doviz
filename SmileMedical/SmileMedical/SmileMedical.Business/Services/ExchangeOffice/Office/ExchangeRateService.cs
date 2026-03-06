using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmileMedical.Business.Infrastructure.ExchangeOffice.Office;
using SmileMedical.Data.Contexts;
using SmileMedical.Entity.Entities.ExchangeOffice.Currency;
using SmileMedical.Entity.Modals.ViewModals.ExchangeOFfice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Services.ExchangeOffice.Office
{
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly SmileMedicalDbContext _context;
        private readonly IMapper mapper;

        public ExchangeRateService(SmileMedicalDbContext context, IMapper mapper)
        {
            _context = context;
            this.mapper = mapper;
        }

        public async Task<vm_exchangerate> GetCurrentRateAsync(Guid officeId, Guid sourceCurrencyId, Guid targetCurrencyId)
        {
            var dbExchanges = await _context.ExchangeRates
                .Where(r => (r.OfficeId == null || r.OfficeId == officeId) &&
                           r.SourceCurrencyId == sourceCurrencyId &&
                           r.TargetCurrencyId == targetCurrencyId &&
                           r.IsActive &&
                           r.EffectiveFrom <= DateTime.Now &&
                           (r.EffectiveTo == null || r.EffectiveTo > DateTime.Now))
                .Include(r => r.Office)
                .Include(r => r.SourceCurrency)
                .Include(r => r.TargetCurrency)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync();

            return mapper.Map<vm_exchangerate>(dbExchanges);
        }

        public async Task<vm_exchangerate> CreateOrUpdateRateAsync(
            Guid officeId,
            Guid sourceCurrencyId,
            Guid targetCurrencyId,
            decimal buyRate,
            decimal sellRate)
        {
            // Validate input
            if (buyRate <= 0 || sellRate <= 0)
                throw new InvalidOperationException("Buy rate and sell rate must be greater than zero");
            
            if (buyRate >= sellRate)
                throw new InvalidOperationException("Buy rate must be less than sell rate");

            // Check if currencies exist
            var sourceExists = await _context.Currencies.AnyAsync(c => c.Id == sourceCurrencyId);
            var targetExists = await _context.Currencies.AnyAsync(c => c.Id == targetCurrencyId);
            
            if (!sourceExists || !targetExists)
                throw new InvalidOperationException("One or both currencies do not exist");

            // Find current active rate for this office
            var currentRate = await _context.ExchangeRates
                .Where(r => r.OfficeId == officeId &&
                           r.SourceCurrencyId == sourceCurrencyId &&
                           r.TargetCurrencyId == targetCurrencyId &&
                           r.IsActive)
                .FirstOrDefaultAsync();

            if (currentRate != null)
            {
                // Check if rates are actually different
                if (currentRate.BuyRate == buyRate && currentRate.SellRate == sellRate)
                {
                    // No change needed, return existing rate
                    return mapper.Map<vm_exchangerate>(currentRate);
                }

                // Deactivate current rate
                currentRate.EffectiveTo = DateTime.Now;
                currentRate.IsActive = false;
                currentRate.UpdatedAt = DateTime.Now;
            }

            // Create new rate
            var newRate = new ExchangeRate
            {
                Id = Guid.NewGuid(),
                OfficeId = officeId,
                SourceCurrencyId = sourceCurrencyId,
                TargetCurrencyId = targetCurrencyId,
                BuyRate = buyRate,
                SellRate = sellRate,
                EffectiveFrom = DateTime.Now,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.ExchangeRates.Add(newRate);
            await _context.SaveChangesAsync();

            // Load related data for response
            await _context.Entry(newRate)
                .Reference(r => r.Office)
                .LoadAsync();
            await _context.Entry(newRate)
                .Reference(r => r.SourceCurrency)
                .LoadAsync();
            await _context.Entry(newRate)
                .Reference(r => r.TargetCurrency)
                .LoadAsync();

            return mapper.Map<vm_exchangerate>(newRate);
        }

        public async Task<vm_exchangerate> UpdateCurrentRateAsync(
            Guid officeId,
            Guid sourceCurrencyId,
            Guid targetCurrencyId,
            decimal buyRate,
            decimal sellRate)
        {
            // Validate input
            if (buyRate <= 0 || sellRate <= 0)
                throw new InvalidOperationException("Buy rate and sell rate must be greater than zero");
            
            if (buyRate >= sellRate)
                throw new InvalidOperationException("Buy rate must be less than sell rate");

            // Find current active rate for this office
            var currentRate = await _context.ExchangeRates
                .Include(r => r.Office)
                .Include(r => r.SourceCurrency)
                .Include(r => r.TargetCurrency)
                .Where(r => r.OfficeId == officeId &&
                           r.SourceCurrencyId == sourceCurrencyId &&
                           r.TargetCurrencyId == targetCurrencyId &&
                           r.IsActive)
                .FirstOrDefaultAsync();

            if (currentRate == null)
                throw new InvalidOperationException($"No active exchange rate found for this currency pair in the specified office");

            // Update the current rate
            currentRate.BuyRate = buyRate;
            currentRate.SellRate = sellRate;
            currentRate.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return mapper.Map<vm_exchangerate>(currentRate);
        }

        public async Task<List<vm_exchangerate>> GetRateHistoryAsync(
            Guid officeId,
            Guid sourceCurrencyId, 
            Guid targetCurrencyId, 
            int? limit = 10)
        {
            var query = _context.ExchangeRates
                .Include(r => r.Office)
                .Include(r => r.SourceCurrency)
                .Include(r => r.TargetCurrency)
                .Where(r => r.OfficeId == officeId &&
                           r.SourceCurrencyId == sourceCurrencyId &&
                           r.TargetCurrencyId == targetCurrencyId)
                .OrderByDescending(r => r.EffectiveFrom)
                .AsQueryable();

            if (limit.HasValue && limit.Value > 0)
                query = query.Take(limit.Value);

            var rates = await query.ToListAsync();

            return mapper.Map<List<vm_exchangerate>>(rates);
        }

        public async Task<List<vm_exchangerate>> GetAllActiveRatesAsync(Guid officeId)
        {

            var dbRates = await _context.ExchangeRates
                .Include(r => r.Office)
                .Include(r => r.SourceCurrency)
                .Include(r => r.TargetCurrency)
                .Where(r => (r.OfficeId == null || r.OfficeId == officeId) &&
                           r.IsActive &&
                           r.EffectiveFrom <= DateTime.Now &&
                           (r.EffectiveTo == null || r.EffectiveTo > DateTime.Now))
                .OrderBy(r => r.SourceCurrency.CurrencyCode)
                .ThenBy(r => r.TargetCurrency.CurrencyCode)
                .AsSplitQuery()
                .AsNoTracking()
                .ToListAsync();

            return mapper.Map<List<vm_exchangerate>>(dbRates);
        }
    }
}
