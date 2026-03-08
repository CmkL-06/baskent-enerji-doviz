using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.ViewModals.Coin
{
    public class vm_coin_profit_stats
    {
        public decimal Yesterday { get; set; }
        public decimal Today { get; set; }
        public decimal ThisWeek { get; set; }
        public decimal LastWeek { get; set; }
        public decimal ThisMonth { get; set; }
        public decimal LastMonth { get; set; }
        public decimal LastYear { get; set; }
        public decimal Total { get; set; }
        
    }
}
