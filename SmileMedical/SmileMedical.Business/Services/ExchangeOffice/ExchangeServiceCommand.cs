using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmileMedical.Business.Infrastructure.ExchangeOffice;
using SmileMedical.Data.Contexts;
using SmileMedical.Entity.Entities.ExchangeOffice.Currency;
using SmileMedical.Entity.Modals.RequestModals.ExchangeService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Services.ExchangeOffice
{
    public class ExchangeServiceCommand : IExchangeServiceCommand
    {
        private readonly SmileMedicalDbContext _dbContext;
        private readonly IMapper mapper;

        public ExchangeServiceCommand(SmileMedicalDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            this.mapper = mapper;
        }

        public async Task DeleteCurrency(Guid currencyId)
        {
            var dbCurrency = _dbContext.Currencies.FirstOrDefault(x => x.Id == currencyId);
            if (dbCurrency != null) _dbContext.Currencies.Remove(dbCurrency);

            await _dbContext.SaveChangesAsync();
           

           
        }

        public async Task DeleteRate(Guid rateId)
        {
            var rate = _dbContext.ExchangeRates.FirstOrDefault(r => r.Id == rateId);
            if (rate != null)
            {
                _dbContext.ExchangeRates.Remove(rate);
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveCurrency(rm_savecurrency data)
        {
            
            var dbCurrency = await _dbContext.Currencies.FirstOrDefaultAsync(x=> x.CurrencyCode == data.CurrencyCode);
            if (dbCurrency != null)
            {
                dbCurrency.CurrencyName = dbCurrency.CurrencyName;
                dbCurrency.CurrencyCode = dbCurrency.CurrencyCode;
            }
            else
            {
                Currency nCurrency = new Currency
                {
                    CreatedDate = DateTime.Now,
                    CurrencyCode = data.CurrencyCode,
                    CurrencyName = data.CurrencyName,
                    Id = Guid.NewGuid(),
                };
                await _dbContext.Currencies.AddAsync(nCurrency);
            }
            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveRate(rm_saveexchangerate data)
        {
            var existing = await _dbContext.ExchangeRates.FirstOrDefaultAsync(r =>
           r.SourceCurrency.Id == data.SourceCurrencyId &&
           r.TargetCurrency.Id == data.TargetCurrencyId);

            if (existing != null)
            {
                existing.BuyRate = data.BuyRate;
                existing.SellRate = data.SellRate;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                data.UpdatedAt = DateTime.UtcNow;
               await _dbContext.ExchangeRates.AddAsync(mapper.Map<ExchangeRate>(data));
            }
            await _dbContext.SaveChangesAsync();
        }
    }
}
