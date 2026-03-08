using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Party
{
    public class PartyContact : BaseEntity
    {
        public Guid PartyId { get; set; }
        public string ContactName { get; set; }
        public string Title { get; set; }
        public string Position { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; } = true;
        public string Notes { get; set; }

        // Navigation properties
        public Party Party { get; set; }
    }
}