using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_monthlystatistics
    {
        public decimal BestDayProfit { get; set; }
        public DateTime BestDayDate { get; set; }
        public decimal WorstDayProfit { get; set; }
        public DateTime WorstDayDate { get; set; }
        public decimal AverageDailyVolume { get; set; }
        public decimal AverageDailyProfit { get; set; }
        public int DaysWithProfit { get; set; }
        public int DaysWithLoss { get; set; }
        public string MostTradedCurrency { get; set; }
        public string MostProfitableCurrency { get; set; }
        public decimal TotalExchangeTransactions { get; set; }
        public decimal TotalDeposits { get; set; }
        public decimal TotalWithdrawals { get; set; }
    }
}