using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_exchangetransaction
    {
        public Guid SourceCurrencyId { get; set; }
        public Guid TargetCurrencyId { get; set; }
        public decimal SourceAmount { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal AppliedRate { get; set; }
        public decimal Commission { get; set; }
        public decimal ProfitLoss { get; set; }

        public decimal? ActualBuyRate { get; set; }
        public decimal? ActualSellRate { get; set; }
      
    }
}
