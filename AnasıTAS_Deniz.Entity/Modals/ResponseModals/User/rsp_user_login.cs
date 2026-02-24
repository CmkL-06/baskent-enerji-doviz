using AnasıTAS_Deniz.Entity.Modals.ViewModals.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.ResponseModals.User
{
    public class rsp_user_login
    {
      public string ApiToken { get; set; }
        public string FirebaseToken { get; set; }
      public vm_user_simple UserInfo { get; set; }
        
    }
}
