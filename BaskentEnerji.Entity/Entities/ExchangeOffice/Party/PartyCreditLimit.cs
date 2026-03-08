using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Party
{
    public class PartyCreditLimit : BaseEntity
    {
        public Guid PartyId { get; set; }
        public Guid CurrencyId { get; set; }
        public decimal CreditLimit { get; set; }
        public decimal UtilizedAmount { get; set; }
        public decimal TemporaryLimit { get; set; }
        public DateTime? TemporaryLimitExpiry { get; set; }
        public int PaymentTermDays { get; set; }
        public decimal InterestRate { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public DateTime LastReviewDate { get; set; }
        public Guid? ApprovedByUserId { get; set; }

        // Computed property
        public decimal AvailableCredit => (CreditLimit + TemporaryLimit) - UtilizedAmount;

        // Navigation properties
        public Party Party { get; set; }
        public Currency.Currency Currency { get; set; }
        public User.User ApprovedByUser { get; set; }
    }
}