using MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Infrastructure.ExchangeOffice.Office
{
    public interface IExchangeReportingService
    {
        Task<vm_monthlyreport> GetMonthlyReport(Guid officeId, int year, int month);
        Task<List<vm_currencyperformance>> GetCurrencyPerformance(Guid officeId, DateTime startDate, DateTime endDate);
        Task<List<vm_vaultutilization>> GetVaultUtilization(Guid? officeId = null);
    }
}
