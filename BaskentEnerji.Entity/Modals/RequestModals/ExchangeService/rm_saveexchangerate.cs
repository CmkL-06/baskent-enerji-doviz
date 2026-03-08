using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService
{
    public class rm_saveexchangerate
    {
        
        public Guid SourceCurrencyId { get; set; }
        public Guid TargetCurrencyId { get; set; }

        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
