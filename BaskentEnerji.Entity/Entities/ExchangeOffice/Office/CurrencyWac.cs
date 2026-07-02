using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using System;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Office
{
    public class CurrencyWac : BaseEntity
    {
        public Guid VaultId { get; set; }
        public Guid CurrencyId { get; set; }
        public decimal Wac { get; set; }
        public decimal Quantity { get; set; }
        public DateTime LastUpdated { get; set; }

        public Vault Vault { get; set; }
        public Currency.Currency Currency { get; set; }
    }
}
