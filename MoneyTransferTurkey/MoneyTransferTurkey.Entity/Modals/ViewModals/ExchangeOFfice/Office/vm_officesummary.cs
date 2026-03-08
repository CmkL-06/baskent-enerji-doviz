using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_officesummary
    {
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; }
        public int VaultCount { get; set; }
        public Dictionary<string, decimal> TotalBalancesByCurrency { get; set; }
        public decimal TotalValueInBaseCurrency { get; set; }
        public decimal DailyProfitLoss { get; set; }
        public decimal MonthlyProfitLoss { get; set; }
    }
}
