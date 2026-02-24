using AnasıTAS_Deniz.Business.Services.ExchangeOffice.Office;
using AnasıTAS_Deniz.Entity.Modals.RequestModals.ExchangeService.Office;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AnasıTAS_Deniz.Business.Infrastructure.ExchangeOffice.Office
{
    public interface IExchangeValidationService
    {
        Task<ExchangeValidationResult> ValidateExchangeTransaction(rm_exchangetransaction request);
        Task<ExchangeValidationResult> ValidateTransfer(rm_transferbetweenvaults request);
    }
}
