using MoneyTransferTurkey.Entity.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.ViewModals.User
{
    public class vm_user_simple : BaseEntity
    {
        public string Username { get; set; }
        public string Mail { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string FullName { get; set; }
        public Rank Rank { get; set; }
        public Gender Gender { get; set; }
        public string LanguageCode { get; set; }
            
    }
}
