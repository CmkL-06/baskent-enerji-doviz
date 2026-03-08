using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Office
{
    public class TransactionDetail: BaseEntity
    {
        public Guid TransactionId { get; set; }
        public Guid CurrencyId { get; set; }
        public TransactionSide Side { get; set; }
        public decimal Amount { get; set; }
        public decimal Rate { get; set; }
        public decimal Commission { get; set; }
        public decimal NetAmount { get; set; } // Amount after commission

        public decimal? ActualBuyRate { get; set; } // Original buy rate from exchange rate table
        public decimal? ActualSellRate { get; set; } // Original sell rate from exchange rate table
        public decimal? CustomRate { get; set; } // Custom rate if used instead of standard rates

        public Transaction Transaction { get; set; }
        public Currency.Currency Currency { get; set; }
    }

    public enum TransactionSide
    {
        Debit = 1,   // Money going out
        Credit = 2   // Money coming in
    }

}

