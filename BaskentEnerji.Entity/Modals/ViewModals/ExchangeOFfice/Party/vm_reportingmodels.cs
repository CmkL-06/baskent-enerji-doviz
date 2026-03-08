using System;
using System.Collections.Generic;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOffice.Party
{
    public class vm_agingreport
    {
        public DateTime AsOfDate { get; set; }
        public List<vm_agedreceivables> Receivables { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalCurrent { get; set; }
        public decimal Total1To30Days { get; set; }
        public decimal Total31To60Days { get; set; }
        public decimal Total61To90Days { get; set; }
        public decimal TotalOver90Days { get; set; }
    }

    public class vm_agingsummary
    {
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; }
        public DateTime AsOfDate { get; set; }
        public int TotalParties { get; set; }
        public decimal TotalOutstanding { get; set; }
        public Dictionary<string, decimal> ByAgeGroup { get; set; }
        public Dictionary<string, decimal> ByCurrency { get; set; }
    }

    public class vm_partyaging
    {
        public Guid PartyId { get; set; }
        public string PartyCode { get; set; }
        public string PartyName { get; set; }
        public decimal TotalOverdue { get; set; }
        public int OldestDaysOverdue { get; set; }
        public DateTime OldestDueDate { get; set; }
        public string ContactPerson { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }

    public class vm_balancereport
    {
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; }
        public DateTime AsOfDate { get; set; }
        public decimal TotalReceivables { get; set; }
        public decimal TotalPayables { get; set; }
        public decimal NetPosition { get; set; }
        public List<vm_partybalance> PartyBalances { get; set; }
    }

    public class vm_reconciliationreport
    {
        public Guid PartyId { get; set; }
        public string PartyName { get; set; }
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public List<vm_partyaccountentry> Entries { get; set; }
        public int ReconciledCount { get; set; }
        public int UnreconciledCount { get; set; }
    }

    public class vm_topparties
    {
        public Guid PartyId { get; set; }
        public string PartyCode { get; set; }
        public string PartyName { get; set; }
        public decimal Balance { get; set; }
        public decimal Percentage { get; set; }
        public int TransactionCount { get; set; }
        public DateTime? LastTransactionDate { get; set; }
    }

    public class vm_transactionreport
    {
        public Guid PartyId { get; set; }
        public string PartyName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public List<vm_transactionreportentry> Transactions { get; set; }
    }

    public class vm_transactionreportentry
    {
        public DateTime Date { get; set; }
        public string TransactionNumber { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
    }

    public class vm_partysummary
    {
        public Guid PartyId { get; set; }
        public string PartyName { get; set; }
        public string PartyType { get; set; }
        public decimal TotalVolume { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageTransactionSize { get; set; }
        public Dictionary<string, decimal> VolumesByCurrency { get; set; }
        public Dictionary<string, decimal> BalancesByCurrency { get; set; }
        public DateTime? FirstTransactionDate { get; set; }
        public DateTime? LastTransactionDate { get; set; }
    }

    public class vm_dailyactivity
    {
        public DateTime Date { get; set; }
        public Guid PartyId { get; set; }
        public string PartyName { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal NetMovement { get; set; }
    }

    public class vm_partyperformance
    {
        public Guid PartyId { get; set; }
        public string PartyName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageBalance { get; set; }
        public decimal PeakBalance { get; set; }
        public int PaymentDelayDays { get; set; }
        public decimal InterestEarned { get; set; }
        public string PerformanceRating { get; set; }
    }

    public class vm_creditperformance
    {
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalCreditExtended { get; set; }
        public decimal TotalCreditUtilized { get; set; }
        public decimal AverageUtilization { get; set; }
        public int DefaultCount { get; set; }
        public decimal DefaultAmount { get; set; }
        public decimal RecoveryRate { get; set; }
    }

    public class vm_overduereport
    {
        public Guid PartyId { get; set; }
        public string PartyCode { get; set; }
        public string PartyName { get; set; }
        public string ContactPerson { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public decimal TotalOverdue { get; set; }
        public List<vm_overdueentry> OverdueEntries { get; set; }
    }

    public class vm_overdueentry
    {
        public Guid EntryId { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime DueDate { get; set; }
        public int DaysOverdue { get; set; }
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}