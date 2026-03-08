using System;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_partycontact
    {
        public Guid Id { get; set; }
        public Guid PartyId { get; set; }
        public string PartyCode { get; set; }
        public string PartyName { get; set; }
        public string ContactName { get; set; }
        public string Position { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }
        public string Status => IsActive ? "Active" : "Inactive";
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}