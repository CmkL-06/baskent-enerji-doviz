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
        public decimal RunningBalance { get; set; }
        public TransactionType TransactionType { get; set; }
        public bool IsParty { get; set; }
        public decimal? AppliedRate { get; set; }
        // Bu bacak kasadan satılan bir döviz ise, satıştan hemen önceki ortalama alış maliyeti (WAC).
        // AppliedRate = satış kuru, CostBasisRate = maliyet (alış) kuru — ikisi arasındaki fark kârı verir.
        public decimal? CostBasisRate { get; set; }
        public decimal? TransactionProfit { get; set; }
    }
}
