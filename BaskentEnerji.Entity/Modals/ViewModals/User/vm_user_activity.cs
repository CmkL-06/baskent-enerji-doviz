using System;
using System.Collections.Generic;

namespace BaskentEnerji.Entity.Modals.ViewModals.User
{
    public class vm_user_activity
    {
        public Guid Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Username { get; set; }
        public string Rank { get; set; }
        public List<string> Offices { get; set; } = new List<string>();
        public DateTime? LastLoginDate { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public bool IsOnline { get; set; }
        public int LoginCountThisMonth { get; set; }
    }
}
