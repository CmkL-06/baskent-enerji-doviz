using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Currency;
using MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService;
using MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOFfice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Infrastructure.ExchangeOffice
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
