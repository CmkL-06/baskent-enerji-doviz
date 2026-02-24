using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Office
{
    public class Office : BaseEntity
    {
        public string OfficeName { get; set; }
        public string? OfficeDescription { get; set; }
        public string? OfficeImageUri { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }

        public bool IsActive { get; set; } = true;
        public ICollection<Vault> Vaults { get; set; }
        public ICollection<User.User> Employees { get; set; }

    }
}
