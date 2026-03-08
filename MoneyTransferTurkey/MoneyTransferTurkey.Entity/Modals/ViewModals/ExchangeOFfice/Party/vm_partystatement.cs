using System;
using System.Collections.Generic;

namespace MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_partystatement
    {
        public Guid Id { get; set; }
        public string StatementNumber { get; set; }
        public DateTime StatementDate { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        
        // Party Information
        public Guid PartyId { get; set; }
        public string PartyCode { get; set; }
        public string PartyName { get; set; }
        public string PartyAddress { get; set; }
        public string PartyEmail { get; set; }
        public string PartyPhone { get; set; }
        public string PartyTaxNumber { get; set; }
        
        // Currency Information
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        public string CurrencySymbol { get; set; }
        
        // Balance Information
        public decimal OpeningBalance { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal ClosingBalance { get; set; }
        
        // Aging Analysis
        public decimal CurrentAmount { get; set; }
        public decimal Amount30Days { get; set; }
        public decimal Amount60Days { get; set; }
        public decimal Amount90Days { get; set; }
        public decimal AmountOver90Days { get; set; }
        
        // Statement Details
        public List<vm_statementline> Lines { get; set; } = new List<vm_statementline>();
        public List<vm_partyaccountentry> Entries { get; set; } = new List<vm_partyaccountentry>();
        
        // Generation Information
        public DateTime GeneratedDate { get; set; }
        public string GeneratedByUserName { get; set; }
        public bool IsSent { get; set; }
        public DateTime? SentDate { get; set; }
        public string SentTo { get; set; }
    }

    public class vm_statementline
    {
        public DateTime Date { get; set; }
        public string EntryNumber { get; set; }
        public string Description { get; set; }
        public string ReferenceNumber { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public decimal Balance { get; set; }
        public string TransactionNumber { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsOverdue => DueDate.HasValue && DueDate < DateTime.UtcNow;
    }
}