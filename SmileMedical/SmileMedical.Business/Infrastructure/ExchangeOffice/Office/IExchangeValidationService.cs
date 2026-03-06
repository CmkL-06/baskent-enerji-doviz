using SmileMedical.Business.Services.ExchangeOffice.Office;
using SmileMedical.Entity.Modals.RequestModals.ExchangeService.Office;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SmileMedical.Business.Infrastructure.ExchangeOffice.Office
{
    public interface IExchangeValidationService
    {
        Task<ExchangeValidationResult> ValidateExchangeTransaction(rm_exchangetransaction request);
        Task<ExchangeValidationResult> ValidateTransfer(rm_transferbetweenvaults request);
    }
}
