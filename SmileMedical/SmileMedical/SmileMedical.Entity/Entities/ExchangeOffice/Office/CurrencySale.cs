using SmileMedical.Entity.Entities.ExchangeOffice.Currency;
using SmileMedical.Entity.Entities.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Entities.ExchangeOffice.Office
{
    public class CurrencySale : BaseEntity
    {
        public Guid VaultId { get; set; }
        public Vault Vault { get; set; }

        public Guid UserId { get; set; }
        public User.User User { get; set; }

        public Guid SourceCurrencyId { get; set; }
        public Currency.Currency SourceCurrency { get; set; }

        public Guid TargetCurrencyId { get; set; }
        public Currency.Currency TargetCurrency { get; set; }

        public decimal SaleRate { get; set; }
        public decimal OriginalSaleRate { get; set; }
        public decimal Revenue {  get; set; }
        public decimal RevenuePercent { get; set; }
        public string? Description { get; set; }
    }
}