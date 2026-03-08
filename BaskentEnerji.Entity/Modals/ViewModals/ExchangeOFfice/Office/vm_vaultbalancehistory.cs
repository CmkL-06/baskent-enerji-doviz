using BaskentEnerji.Entity.Entities;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_vaultbalancehistory : BaseEntity
    {
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        public string? Description { get; set; }
        public string? User { get; set; }
        public decimal Balance { get; set; }
        public decimal AvailableBalance => Balance;
        public decimal ValueInBaseCurrency { get; set; }
        public TransactionType TransactionType { get; set; }
        public bool IsParty { get; set; }
    }
}
