using AnasıTAS_Deniz.Entity.Entities;
using AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Currency;
using AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_transactiondetail
    {

        public Guid TransactionId { get; set; }
        public Guid CurrencyId { get; set; }
        public TransactionSide Side { get; set; }
        public decimal Amount { get; set; }
        public decimal Rate { get; set; }

        public decimal? ActualBuyRate { get; set; }
        public decimal? ActualSellRate { get; set; }
        public decimal? CustomRate { get; set; }
        public decimal Commission { get; set; }
        public decimal NetAmount { get; set; } 
        public string Username { get; set; }
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }


        public DateTime TransactionDate { get; set; }
        public TransactionStatus Status { get; set; }
        public string CurrencyName { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencySymbol { get; set; }

    }

   
}

