using AnasıTAS_Deniz.Data.Migrations;
using AnasıTAS_Deniz.Entity.Modals.RequestModals.ExchangeService.Office;
using AnasıTAS_Deniz.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace AnasıTAS_Deniz.Business.Infrastructure.ExchangeOffice.Office
{
    public interface IExchangeTransactionService
    {
        Task<List<vm_exchangetransaction>> ProcessExchangeAsync(List<rm_exchangetransaction> request);
        Task<Entity.Entities.ExchangeOffice.Office.Transaction> TransferBetweenVaultsAsync(rm_transferbetweenvaults request);
        Task<decimal> CalculateProfitLossAsync(Guid officeId, DateTime? startDate, DateTime? endDate);
        Task<List<vm_transaction>> GetTransactionHistoryAsync(Guid? vaultId, DateTime? startDate, DateTime? endDate);
        Task<vm_transaction> GetTransaction(Guid id);

        Task RemoveTransaction(rm_removetransaction request);
    }
}
