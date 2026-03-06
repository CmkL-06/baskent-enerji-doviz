using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Modals.RequestModals.ExchangeService.Office
{
    public class rm_transferbetweenvaults
    {
        public Guid SourceVaultId { get; set; }
        public Guid TargetVaultId { get; set; }
        public Guid CurrencyId { get; set; }
        public decimal Amount { get; set; }
        public string Notes { get; set; }
    }
}
