using System;
using System.Collections.Generic;

namespace MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Office
{
    public class rm_vaultcount
    {
        public Guid VaultId { get; set; }
        public List<rm_vaultcountdetail> CountDetails { get; set; }
        public bool IsManual { get; set; } // true if manually triggered by employee
    }

    public class rm_vaultcountdetail
    {
        public Guid CurrencyId { get; set; }
        public decimal ActualAmount { get; set; } // Amount entered by employee
    }
}