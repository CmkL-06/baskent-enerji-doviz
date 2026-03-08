using MoneyTransferTurkey.Entity.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.ViewModals.User
{
    public class vm_user : BaseEntity
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Mail { get; set; }
        public bool IsEmailVerified { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public Gender Gender { get; set; }
        public string Rank { get; set; }
    
        public string LanguageCode { get; set; }
        public string? FirstIp { get; set; }
        public string? LastIp { get; set; }
    }
}
