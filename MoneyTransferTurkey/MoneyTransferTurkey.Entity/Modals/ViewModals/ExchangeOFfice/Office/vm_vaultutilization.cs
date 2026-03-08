using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_vaultutilization
    {
        public Guid VaultId { get; set; }
        public string VaultName { get; set; }
        public string OfficeName { get; set; }
        public Dictionary<string, decimal> CurrentBalances { get; set; }
        public Dictionary<string, decimal> AverageBalances { get; set; }
        public Dictionary<string, decimal> UtilizationRates { get; set; }
        public decimal TotalValueInBaseCurrency { get; set; }
    }
}
