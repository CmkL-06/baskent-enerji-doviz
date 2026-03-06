using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_currencyperformance
    {
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalProfit { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageSpread { get; set; }
    }
}
