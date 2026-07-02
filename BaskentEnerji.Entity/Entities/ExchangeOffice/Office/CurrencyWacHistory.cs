using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using System;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Office
{
    public class CurrencyWacHistory : BaseEntity
    {
        public Guid VaultId { get; set; }
        public Guid CurrencyId { get; set; }
        public decimal OldWac { get; set; }
        public decimal NewWac { get; set; }
        public decimal OldQuantity { get; set; }
        public decimal NewQuantity { get; set; }
        public decimal TransactionAmount { get; set; }
        public decimal TransactionRate { get; set; }
        public Guid? TransactionId { get; set; }
        public WacChangeReason Reason { get; set; }

        public Vault Vault { get; set; }
        public Currency.Currency Currency { get; set; }
        public Transaction Transaction { get; set; }
    }

    public enum WacChangeReason
    {
        Purchase = 1,
        DayOpening = 2,
        Adjustment = 3,
        Transfer = 4,
        InitialSeed = 5
    }
}
