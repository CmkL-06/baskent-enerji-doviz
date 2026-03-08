using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.RequestModals.User
{
    public class rm_change_user_password
    {
        public Guid UserId { get; set; }
        public string NewPassword { get; set; }
    }
}
