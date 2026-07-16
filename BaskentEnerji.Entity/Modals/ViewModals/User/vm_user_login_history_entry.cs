using System;

namespace BaskentEnerji.Entity.Modals.ViewModals.User
{
    public class vm_user_login_history_entry
    {
        public DateTime LoginDate { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }
}
