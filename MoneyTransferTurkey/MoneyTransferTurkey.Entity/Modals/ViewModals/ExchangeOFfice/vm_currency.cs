using MoneyTransferTurkey.Entity.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOFfice
{
    public class vm_currency : BaseEntity
    {
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; } = string.Empty;
        public string CurrencySymbol { get; set; } = string.Empty;
    }
}
