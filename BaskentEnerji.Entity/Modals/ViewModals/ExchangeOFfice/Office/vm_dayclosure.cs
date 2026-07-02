using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using System;
using System.Collections.Generic;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_daystatus
    {
        public Guid OfficeId { get; set; }
        public bool IsDayOpen { get; set; }
        public DateTime? CurrentBusinessDate { get; set; }
        public DateTime? LastClosedDate { get; set; }
        public bool HasUnclosedDays { get; set; }
        public int UnclosedDayCount { get; set; }
        public DateTime? FirstUnclosedDate { get; set; }
        public bool CanTransact { get; set; }
        public string BlockReason { get; set; }
        public List<vm_dayclosurebalance> SystemBalances { get; set; }
    }

    public class vm_dayclosurebalance
    {
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        public decimal SystemBalance { get; set; }
        public decimal CurrentWac { get; set; }
    }

    public class vm_dayclosure
    {
        public Guid Id { get; set; }
        public Guid OfficeId { get; set; }
        public DateTime BusinessDate { get; set; }
        public DateTime ClosedAt { get; set; }
        public string ClosedByUser { get; set; }
        public DayClosureStatus Status { get; set; }
        public decimal TotalRealizedProfit { get; set; }
        public int TransactionCount { get; set; }
        public bool HasDiscrepancy { get; set; }
        public string Notes { get; set; }
        public List<vm_dayclosuredetail> Details { get; set; }
    }

    public class vm_dayclosuredetail
    {
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public decimal SystemBalance { get; set; }
        public decimal PhysicalCount { get; set; }
        public decimal Discrepancy { get; set; }
        public string DiscrepancyNote { get; set; }
        public decimal WacAtClose { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal OpeningWac { get; set; }
    }

    public class vm_consolidated_dayclosure
    {
        public DateTime BusinessDate { get; set; }
        public int TotalOffices { get; set; }
        public int ClosedOffices { get; set; }
        public int UnclosedOffices { get; set; }
        public decimal TotalRealizedProfit { get; set; }
        public int TotalTransactionCount { get; set; }
        public bool HasAnyDiscrepancy { get; set; }
        public List<vm_office_closure_summary> Offices { get; set; } = new();
    }

    public class vm_office_closure_summary
    {
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; } = "";
        public int OfficeType { get; set; }
        public bool IsClosed { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string ClosedByUser { get; set; } = "";
        public decimal RealizedProfit { get; set; }
        public int TransactionCount { get; set; }
        public bool HasDiscrepancy { get; set; }
        public decimal TotalDiscrepancyValue { get; set; }
    }
}
