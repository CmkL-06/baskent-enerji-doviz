using SmileMedical.Entity.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Modals.RequestModals.User
{
    public class rm_user_update : BaseEntity
    {
        public string Username { get; set; }
        public string Mail { get; set; }
        public bool IsEmailVerified { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public Gender Gender { get; set; }
        public Rank Rank { get; set; } = Rank.User;
    }
}
