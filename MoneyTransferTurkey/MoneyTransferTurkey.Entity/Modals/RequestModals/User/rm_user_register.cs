using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.RequestModals.User
{
    public class rm_user_register
    {
        public string Username{ get; set; }
        public string Mail{ get; set; }
        public string Password{ get; set; }
        public string? Firstname{ get; set; }
        public string? Lastname{ get; set; }
        public Gender Gender{ get; set; }
    }
}
