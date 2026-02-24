using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.User
{
    public class rm_user_get
    {
        public Guid? Id { get; set; }
        public string? Username { get; set; }
        public string? Mail { get; set; }
        public Rank? Rank { get; set; }
    }
}
