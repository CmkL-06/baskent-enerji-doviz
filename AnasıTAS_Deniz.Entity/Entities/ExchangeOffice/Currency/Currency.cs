using AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Currency
{
    public class Currency : BaseEntity
    {
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; } = string.Empty;
        public string CurrencySymbol { get; set; } = string.Empty;

        public int DecimalPlaces { get; set; } = 2;

        public ICollection<ExchangeRate> SourceRates { get; set; }
        public ICollection<ExchangeRate> TargetRates { get; set; }
        public ICollection<VaultBalance> VaultBalances { get; set; }

    }
}
