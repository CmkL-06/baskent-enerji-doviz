using MoneyTransferTurkey.Entity.Modals.RequestModals.User;
using MoneyTransferTurkey.Entity.Modals.ViewModals.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Infrastructure.User
{
    public interface IUserServiceQuery
    {
        Task<vm_user> GetUser (rm_user_get FilterData);
        Task<List<vm_user>> GetUsers (rm_user_get? FilterData);
      
    }
}
