using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.RequestModals.User
{
    public class rm_user_login
    {
        /// <summary>Email veya kullanici adi — ikisi de kabul edilir</summary>
        public string? Mail { get; set; }
        public string? Password { get; set; }
    }
}
