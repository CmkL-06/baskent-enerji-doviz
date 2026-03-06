using SmileMedical.Entity.Entities.ExchangeOffice.Currency;
using SmileMedical.Entity.Modals.ViewModals.ExchangeOFfice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Infrastructure.ExchangeOffice.Office
{
    public interface IExchangeRateService
    {
        Task<vm_exchangerate> GetCurrentRateAsync(Guid officeId, Guid sourceCurrencyId, Guid targetCurrencyId);
        Task<vm_exchangerate> CreateOrUpdateRateAsync(Guid officeId, Guid sourceCurrencyId, Guid targetCurrencyId, decimal buyRate, decimal sellRate);
        Task<vm_exchangerate> UpdateCurrentRateAsync(Guid officeId, Guid sourceCurrencyId, Guid targetCurrencyId, decimal buyRate, decimal sellRate);
        Task<List<vm_exchangerate>> GetAllActiveRatesAsync(Guid officeId);
        Task<List<vm_exchangerate>> GetRateHistoryAsync(Guid officeId, Guid sourceCurrencyId, Guid targetCurrencyId, int? limit = 10);
    }
}
