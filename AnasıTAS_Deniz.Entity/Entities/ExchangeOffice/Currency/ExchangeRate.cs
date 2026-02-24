using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Currency
{
    public class ExchangeRate : BaseEntity
    {
        public Guid? OfficeId { get; set; }
        public Guid SourceCurrencyId { get; set; }
        public Guid TargetCurrencyId { get; set; }
        public decimal BuyRate { get; set; }  
        public decimal SellRate { get; set; } 
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;

      
        public Office.Office Office { get; set; }
        public Currency SourceCurrency { get; set; }
        public Currency TargetCurrency { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

}
