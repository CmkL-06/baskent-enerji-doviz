using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Entities.ExchangeOffice.Party
{
    public class Party : BaseEntity
    {
        public Guid OfficeId { get; set; }
        public string PartyCode { get; set; }
        public string Name { get; set; }
        public PartyType Type { get; set; }
        public PartyStatus Status { get; set; } = PartyStatus.Active;
        public string? ContactPerson { get; set; }
        public string? TaxNumber { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public bool HasCreditLimit { get; set; }
        public int DefaultPaymentTermDays { get; set; } = 0;
        public DateTime? LastTransactionDate { get; set; }
        public decimal TotalVolume { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public Guid? ModifiedByUserId { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        public Office.Office Office { get; set; }
        public User.User CreatedBy { get; set; }
        public User.User ModifiedBy { get; set; }
        public ICollection<PartyAccount> Accounts { get; set; }
        public ICollection<PartyContact> Contacts { get; set; }
        public ICollection<PartyCreditLimit> CreditLimits { get; set; }
        public ICollection<Office.Transaction> Transactions { get; set; }
    }

    public enum PartyType
    {
        Customer = 1,
        Supplier = 2,
        Both = 3
    }

    public enum PartyStatus
    {
        Active = 1,
        Inactive = 2,
        Suspended = 3,
        Blocked = 4
    }
}