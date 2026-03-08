using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_monthlyreport
    {
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal AverageProfit { get; set; }
        public decimal ProfitMargin { get; set; } // Profit as percentage of volume
        public Dictionary<string, decimal> VolumesByCurrency { get; set; }
        public Dictionary<string, decimal> ProfitsByCurrency { get; set; }
        public List<vm_dailyperformance> DailyPerformance { get; set; }
        public List<vm_currencyperformance> CurrencyPerformance { get; set; }
        public vm_monthlystatistics Statistics { get; set; }
    }
}
