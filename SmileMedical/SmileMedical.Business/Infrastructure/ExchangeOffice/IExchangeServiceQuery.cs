using SmileMedical.Entity.Entities.ExchangeOffice.Currency;
using SmileMedical.Entity.Modals.RequestModals.ExchangeService;
using SmileMedical.Entity.Modals.ViewModals.ExchangeOFfice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Infrastructure.ExchangeOffice
{
    public interface IExchangeServiceQuery
    {
        Task<ExchangeRate?> GetRate(Guid sourceCurrencyId, Guid targetCurrencyId);
        Task<decimal> Convert(rm_convertcurrency data);
        Task<List<vm_exchangerate>> GetAllRates();
        List<vm_currency> GetAllCurrencies();
        vm_currency GetCurrencyById(Guid currencyId);
    }

}
