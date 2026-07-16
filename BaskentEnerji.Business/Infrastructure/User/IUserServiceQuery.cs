using BaskentEnerji.Entity.Modals.RequestModals.User;
using BaskentEnerji.Entity.Modals.ViewModals.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.User
{
    public interface IUserServiceQuery
    {
        Task<vm_user> GetUser (rm_user_get FilterData);
        Task<List<vm_user>> GetUsers (rm_user_get? FilterData);
        Task<List<vm_user_activity>> GetUserActivityList();
        Task<List<vm_user_login_history_entry>> GetUserLoginHistory(Guid userId, int? year, int? month);
        Task<vm_user_daily_detail> GetUserDailyDetail(Guid userId, DateTime date);

    }
}
