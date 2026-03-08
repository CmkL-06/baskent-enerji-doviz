using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Party
{
    public class PartyStatement : BaseEntity
    {
        public Guid PartyId { get; set; }
        public Guid CurrencyId { get; set; }
        public string StatementNumber { get; set; }
        public DateTime StatementDate { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        
        // Aging Analysis Fields
        public decimal CurrentAmount { get; set; }
        public decimal Amount30Days { get; set; }
        public decimal Amount60Days { get; set; }
        public decimal Amount90Days { get; set; }
        public decimal AmountOver90Days { get; set; }
        
        public string Notes { get; set; }
        public StatementStatus Status { get; set; }
        public DateTime? SentDate { get; set; }
        public string SentTo { get; set; }
        public Guid GeneratedByUserId { get; set; }

        // Navigation properties
        public Party Party { get; set; }
        public Currency.Currency Currency { get; set; }
        public User.User GeneratedByUser { get; set; }
    }

    public enum StatementStatus
    {
        Draft = 1,
        Final = 2,
        Sent = 3,
        Acknowledged = 4
    }
}