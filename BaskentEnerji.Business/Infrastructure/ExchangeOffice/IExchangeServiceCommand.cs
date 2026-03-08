using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.ExchangeOffice
{
    public interface IExchangeServiceCommand
    {
        Task DeleteCurrency(Guid currencyId);
        Task SaveCurrency(rm_savecurrency data);
        Task SaveRate(rm_saveexchangerate data);
        Task DeleteRate(Guid rateId);
    }
}
