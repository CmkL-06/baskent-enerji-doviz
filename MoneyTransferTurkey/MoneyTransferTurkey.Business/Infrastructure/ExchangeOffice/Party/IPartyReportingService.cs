using MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOffice.Party;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Infrastructure.ExchangeOffice.Party
{
    public interface IPartyReportingService
    {
        // Statement Generation
        Task<vm_partystatement> GenerateStatementAsync(Guid partyId, Guid currencyId, DateTime fromDate, DateTime toDate);
        Task<vm_partystatement> GetStatementAsync(Guid statementId);
        Task<List<vm_partystatement>> GetStatementsAsync(Guid partyId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<bool> MarkStatementAsSentAsync(Guid statementId, string sentTo);
        
        // Aging Analysis
        Task<vm_agingreport> GetAgingReportAsync(Guid? partyId = null, Guid? officeId = null, DateTime? asOfDate = null);
        Task<vm_agingsummary> GetAgingSummaryAsync(Guid officeId, DateTime? asOfDate = null);
        Task<List<vm_partyaging>> GetPartiesWithOverdueBalancesAsync(Guid officeId, int daysOverdue);
        
        // Balance Reports
        Task<List<vm_agedreceivables>> GetAgedReceivablesAsync(Guid officeId, DateTime asOfDate);
        Task<List<vm_partybalance>> GetPartyBalanceSummaryAsync(Guid officeId);
        Task<vm_balancereport> GetBalanceReportAsync(Guid officeId, DateTime? asOfDate = null);
        Task<vm_reconciliationreport> GetReconciliationReportAsync(Guid partyId, Guid currencyId, DateTime fromDate, DateTime toDate);
        Task<List<vm_topparties>> GetTopPartiesByBalanceAsync(Guid officeId, int topCount = 10, bool receivables = true);
        
        // Transaction Reports
        Task<vm_transactionreport> GetTransactionReportAsync(Guid partyId, DateTime fromDate, DateTime toDate);
        Task<vm_partysummary> GetPartySummaryAsync(Guid partyId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<vm_dailyactivity>> GetDailyActivityReportAsync(Guid officeId, DateTime date);
        
        // Performance Reports
        Task<vm_partyperformance> GetPartyPerformanceAsync(Guid partyId, DateTime fromDate, DateTime toDate);
        Task<vm_creditperformance> GetCreditPerformanceReportAsync(Guid officeId, DateTime fromDate, DateTime toDate);
        Task<List<vm_overduereport>> GetOverdueReportAsync(Guid officeId);
        
        // Export Functions
        Task<byte[]> ExportStatementToPdfAsync(Guid statementId);
        Task<byte[]> ExportAgingReportToExcelAsync(Guid officeId, DateTime asOfDate);
        Task<byte[]> ExportBalanceReportToExcelAsync(Guid officeId, DateTime asOfDate);
    }
}