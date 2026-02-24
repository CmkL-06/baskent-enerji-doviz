using AnasıTAS_Deniz.Entity.Modals.RequestModals.User;
using AnasıTAS_Deniz.Entity.Modals.ViewModals.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Infrastructure.User
{
    public interface IUserServiceQuery
    {
        Task<vm_user> GetUser (rm_user_get FilterData);
        Task<List<vm_user>> GetUsers (rm_user_get? FilterData);
      
    }
}
