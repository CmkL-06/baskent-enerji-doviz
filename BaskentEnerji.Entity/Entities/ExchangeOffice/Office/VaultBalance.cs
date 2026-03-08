using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Office
{
    public class VaultBalance : BaseEntity
    {
        public Guid VaultId { get; set; }
        public Guid CurrencyId { get; set; }
        public decimal Balance { get; set; }
        public decimal ReservedAmount { get; set; } // For pending transactions
        public DateTime LastUpdated { get; set; }
        
        public Vault Vault { get; set; }
        public Currency.Currency Currency { get; set; }
    }
}
