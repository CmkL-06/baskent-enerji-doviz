using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office
{
    public interface IZReportService
    {
        Task<vm_zreport> GetDailyZReport(Guid? officeId, DateTime date);
        Task<vm_zreport> GetWeeklyZReport(Guid? officeId, DateTime weekStartDate);
        Task<vm_zreport> GetMonthlyZReport(Guid? officeId, int year, int month);
        Task<vm_zreport> GetYearlyZReport(Guid? officeId, int year);
        Task<vm_zreport> GetCustomPeriodZReport(Guid? officeId, DateTime startDate, DateTime endDate);
        Task<List<vm_zreport>> GetHistoricalZReports(Guid? officeId, ZReportPeriod period, int count);
        Task EndDay(Guid officeId);
        void InvalidateDailyZReportCache(Guid? officeId, DateTime date);
    }
}