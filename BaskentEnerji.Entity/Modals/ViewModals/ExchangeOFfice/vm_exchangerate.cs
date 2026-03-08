using BaskentEnerji.Entity.Entities;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice
{
    public class vm_exchangerate : BaseEntity
    {
        public Guid? OfficeId { get; set; }
        public string OfficeName { get; set; }
        
        public Guid SourceCurrencyId { get; set; }
        public string SourceCurrencyName { get; set; }
        public string SourceCurrencyCode { get; set; }

        public Guid TargetCurrencyId { get; set; }
        public string TargetCurrencyName { get; set; }
        public string TargetCurrencyCode { get; set; }

        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; }
    }
}
