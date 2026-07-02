using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office
{
    public interface IDayClosureService
    {
        Task<vm_daystatus> GetDayStatusAsync(Guid officeId);
        Task<vm_dayclosure> CloseDayAsync(rm_dayclosure request);
        Task<bool> CanTransactAsync(Guid officeId);
        Task<List<vm_dayclosure>> GetClosureHistoryAsync(Guid officeId, DateTime? startDate = null, DateTime? endDate = null);
        Task<vm_dayclosure> GetDayClosureAsync(Guid officeId, DateTime businessDate);
        Task<vm_consolidated_dayclosure> GetConsolidatedDayClosureAsync(DateTime businessDate);
    }
}
