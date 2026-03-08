using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_dailyperformance
    {
        public DateTime Date { get; set; }
        public int TransactionCount { get; set; }
        public int ExchangeCount { get; set; }
        public int DepositCount { get; set; }
        public int WithdrawalCount { get; set; }
        public decimal Volume { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMargin { get; set; }
        public decimal AverageTransactionSize { get; set; }
    }
}
