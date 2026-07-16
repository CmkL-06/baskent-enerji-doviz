using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_zreport
    {
        public Guid? OfficeId { get; set; }
        public string OfficeName { get; set; }
        public DateTime ReportDate { get; set; }
        public ZReportPeriod Period { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public bool IsVaultOpen { get; set; }
        
        // Section 1: Total Summary
        public vm_zreport_summary Summary { get; set; }
        
        // Section 2: Currency Details
        public List<vm_zreport_currency_detail> CurrencyDetails { get; set; }
        
        // Section 3: Party Accounts Summary (optional)
        public vm_zreport_party_summary PartyAccountsSummary { get; set; }
        
        // Section 4: Summary without Party Debts/Receivables
        public vm_zreport_cash_summary CashOnlySummary { get; set; }
        
        // Section 5: Vault Balance Histories (All vault operations in the period)
        public List<vm_vaultbalancehistory> VaultBalanceHistories { get; set; }
        
        // Multi-office aggregated data (when no specific office is selected)
        public List<vm_zreport_office_summary> OfficeBreakdown { get; set; }

        // Section 6: Per-employee transaction breakdown (single-office view only —
        // boş liste döner "Tüm Şubeler" modunda, bkz. GenerateMultiOfficeReport)
        public List<vm_zreport_employee_summary> EmployeeBreakdown { get; set; }
    }
    
    public class vm_zreport_summary
    {
        public decimal TotalProfit { get; set; }
        public decimal TotalProfitInTRY { get; set; }
        public int TotalTransactions { get; set; }
        public int TotalExchangeTransactions { get; set; }
        public int TotalDepositTransactions { get; set; }
        public int TotalWithdrawalTransactions { get; set; }
        public decimal TotalForeignCurrencyProcessed { get; set; } // Total volume in TRY
        public Dictionary<string, decimal> TotalVolumesByCurrency { get; set; } // Raw amounts per currency
        public decimal AverageTransactionSize { get; set; }
        public decimal ProfitMargin { get; set; } // Percentage

        // Arbitraj (çapraz kur, iki yabancı bacaklı) işlemlerin kârı — tek bir para birimine
        // atanamayacağından (her iki bacak da yabancı para) burada ayrıca toplanır.
        public decimal TotalArbitrageProfit { get; set; }

        // Vault Operations Summary
        public decimal VaultDeposits { get; set; } // Total deposits to vault in TRY
        public decimal VaultWithdrawals { get; set; } // Total withdrawals from vault in TRY
        public decimal NetVaultChange { get; set; } // Deposits - Withdrawals
        public decimal ProfitAfterVaultOperations { get; set; } // TotalProfit + NetVaultChange
        
        // Opening Balance Info (for new vaults that inherited from previous day)
        public decimal OpeningBalanceTRY { get; set; } // TRY balance from previous vault
        public bool HasInheritedBalance { get; set; } // Indicates if this vault started with inherited balance
        
        // Total Value in Base Currency (TRY) - All currencies converted to TRY
        public decimal TotalValueInBaseCurrency { get; set; } // Total value of all vault balances in TRY
    }
    
    public class vm_zreport_currency_detail
    {
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        
        // Purchase (Buy) Details
        public decimal TotalBoughtAmount { get; set; }
        public decimal TotalBuyCost { get; set; } // Total cost in TRY
        public decimal AverageBuyRate { get; set; }
        public int BuyTransactionCount { get; set; }
        
        // Sale (Sell) Details
        public decimal TotalSoldAmount { get; set; }
        public decimal TotalSellRevenue { get; set; } // Total revenue in TRY
        public decimal AverageSellRate { get; set; }
        public int SellTransactionCount { get; set; }
        
        // Profit & Performance
        public decimal Profit { get; set; } // Profit in TRY
        public decimal ProfitMargin { get; set; } // Percentage
        public decimal NetPosition { get; set; } // Bought - Sold (inventory change)
        
        // WAC (Weighted Average Cost)
        public decimal Wac { get; set; }
        public decimal RealizedProfit { get; set; }
        public decimal UnrealizedProfit { get; set; }
        public decimal CurrentBalance { get; set; }

        // Current Rates (for reference)
        public decimal CurrentBuyRate { get; set; }
        public decimal CurrentSellRate { get; set; }
        public decimal Spread { get; set; }
    }
    
    public class vm_zreport_party_summary
    {
        public int TotalPartyAccounts { get; set; }
        public int ActivePartyAccounts { get; set; }
        
        // Debts (We owe to parties)
        public Dictionary<string, decimal> TotalDebtsByCurrency { get; set; }
        public decimal TotalDebtsInTRY { get; set; }
        
        // Receivables (Parties owe to us)
        public Dictionary<string, decimal> TotalReceivablesByCurrency { get; set; }
        public decimal TotalReceivablesInTRY { get; set; }
        
        // Net Position
        public decimal NetPositionInTRY { get; set; } // Receivables - Debts
        
        // Today's Party Transactions
        public int PartyTransactionCount { get; set; }
        public decimal PartyTransactionVolume { get; set; }
    }
    
    public class vm_zreport_cash_summary
    {
        // Summary excluding party account transactions
        public decimal CashProfit { get; set; }
        public decimal CashVolumeInTRY { get; set; }
        public int CashTransactionCount { get; set; }
        public Dictionary<string, decimal> CashVolumesByCurrency { get; set; }
        
        // Vault Balances (actual cash on hand)
        public Dictionary<string, decimal> VaultBalancesByCurrency { get; set; }
        public decimal TotalVaultValueInTRY { get; set; }
    }
    
    public class vm_zreport_employee_summary
    {
        public Guid UserId { get; set; }
        public string EmployeeName { get; set; } // Firstname + " " + Lastname
        public int TransactionCount { get; set; }
        public int ExchangeTransactionCount { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalVolumeInTRY { get; set; }
        public decimal AverageTransactionSize { get; set; }
    }

    public class vm_zreport_office_summary
    {
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; }
        public decimal Profit { get; set; }
        public int TransactionCount { get; set; }
        public decimal VolumeInTRY { get; set; }
        public bool IsVaultOpen { get; set; }
        public decimal ContributionPercentage { get; set; } // % of total profit
    }
    
    public enum ZReportPeriod
    {
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Yearly = 4
    }
}