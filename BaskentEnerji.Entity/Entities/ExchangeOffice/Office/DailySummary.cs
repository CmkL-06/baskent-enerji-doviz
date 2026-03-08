using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Office
{
    public class DailySummary : BaseEntity
    {
        public Guid OfficeId { get; set; }
        public Guid VaultId { get; set; }
        public DateTime SummaryDate { get; set; }
        public Guid CurrencyId { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalIn { get; set; }
        public decimal TotalOut { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal ProfitLoss { get; set; }
        public int TransactionCount { get; set; }

       
        public Office Office { get; set; }
        public Vault Vault { get; set; }
        public Currency.Currency Currency { get; set; }
    }
}
