using AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Currency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Office
{
    public class Vault: BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid OfficeId { get; set; }
        public VaultType Type { get; set; } = VaultType.Main;
        public bool IsActive { get; set; } = true;
        public decimal ClosingBalance { get; set; } = 0; // TRY balance when vault was closed
        public DateTime? ClosedDate { get; set; } // Date when vault was closed
        public bool ShouldCount { get; set; } = false;
        public DateTime? LastCountDate { get; set; }

       
        public Office Office { get; set; }
        public ICollection<VaultBalance> Balances { get; set; }
        public ICollection<Transaction> Transactions { get; set; }


        public enum VaultType
        {
            Main = 1,
            Secondary = 2,
            Reserve = 3
        }
    }
}
