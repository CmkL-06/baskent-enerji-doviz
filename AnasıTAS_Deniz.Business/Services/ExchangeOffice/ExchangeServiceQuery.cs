using AutoMapper;
using Microsoft.EntityFrameworkCore;
using AnasıTAS_Deniz.Business.Infrastructure.ExchangeOffice;
using AnasıTAS_Deniz.Data.Contexts;
using AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Currency;
using AnasıTAS_Deniz.Entity.Modals.RequestModals.ExchangeService;
using AnasıTAS_Deniz.Entity.Modals.ViewModals.ExchangeOFfice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Services.ExchangeOffice
{
    public class ExchangeServiceQuery : IExchangeServiceQuery
    {
        private readonly AnasıTAS_DenizDbContext _dbContext;
        private readonly IMapper mapper;

        public ExchangeServiceQuery(AnasıTAS_DenizDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            this.mapper = mapper;
        }



        public async Task<decimal> Convert(rm_convertcurrency data)
        {
            var rate = await GetRate(data.sourceCurrencyId, data.targetCurrencyId);
            if (rate == null)
                throw new Exception("Exchange rate not found");

            return data.isBuyingFromCustomer
                ? data.amount * rate.SellRate
                : data.amount * rate.BuyRate;
        }

        public List<vm_currency> GetAllCurrencies()
        {
            return mapper.Map<List<vm_currency>>(_dbContext.Currencies.ToList());
        }

        public async Task<List<vm_exchangerate>> GetAllRates()
        {
            return mapper.Map<List<vm_exchangerate>>(await _dbContext.ExchangeRates
                .Include(x => x.Office)
                .Include(x => x.TargetCurrency)
                .Include(x => x.SourceCurrency)
                .Where(x => x.IsActive)
                .ToListAsync());
        }

        public vm_currency GetCurrencyById(Guid currencyId)
        {
            return mapper.Map<vm_currency>(_dbContext.Currencies.FirstOrDefault(x => x.Id == currencyId));
        }

        public Task<ExchangeRate?> GetRate(Guid sourceCurrencyId, Guid targetCurrencyId)
        {
            var rate = _dbContext.ExchangeRates.FirstOrDefault(r =>
            r.SourceCurrency.Id == sourceCurrencyId &&
            r.TargetCurrency.Id == targetCurrencyId);

            return Task.FromResult(rate);
        }
    }
}
