using MoneyTransferTurkey.Entity.Entities;
using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Party;
using System;
using System.Collections.Generic;

namespace MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_party 
    {
       public Guid Id { get; set; }
        public string PartyCode { get; set; }
        public string Name { get; set; }
        public PartyType Type { get; set; }
        public string TypeName { get; set; }
        public string ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public string? RegistrationNumber { get; set; }
        public bool HasCreditLimit { get; set; }
        public int DefaultPaymentTermDays { get; set; }
        public PartyStatus Status { get; set; }
        public string StatusName { get; set; }
        public DateTime? LastTransactionDate { get; set; }
        public string? Notes { get; set; }
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedByUserName { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedByUserName { get; set; }
        
        // Summary information
        public int AccountCount { get; set; }
        public int ActiveCreditLimitCount { get; set; }
        public decimal TotalReceivables { get; set; }
        public decimal TotalPayables { get; set; }
        public decimal NetBalance { get; set; }
        
        // Related data
        public List<vm_partyaccount> Accounts { get; set; }
        public List<vm_partycreditlimit> CreditLimits { get; set; }
        public List<vm_partycontact> Contacts { get; set; }
       
    }
}