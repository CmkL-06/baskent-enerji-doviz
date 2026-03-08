using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office
{
    public class rm_exchangetransaction
    {
        public Guid VaultId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? PartyId { get; set; }
        public Guid SourceCurrencyId { get; set; }
        public Guid TargetCurrencyId { get; set; }
        public decimal SourceAmount { get; set; }
        public bool IsBuyingFromCustomer { get; set; } // true = customer selling to us
        public decimal? CustomRate { get; set; } 
       // public decimal CommissionPercent { get; set; } = 0;
        public string? Notes { get; set; }


    }
}
