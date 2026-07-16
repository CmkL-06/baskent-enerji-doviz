using System;
using System.Collections.Generic;

namespace BaskentEnerji.Entity.Modals.ViewModals.User
{
    public class vm_user_session
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    public class vm_user_day_closure_status
    {
        public string OfficeName { get; set; }
        public string Status { get; set; }
        public string ClosedByUser { get; set; }
        public DateTime? ClosedAt { get; set; }
    }

    public class vm_user_daily_detail
    {
        public List<vm_user_session> Sessions { get; set; } = new List<vm_user_session>();
        public List<vm_user_day_closure_status> DayClosures { get; set; } = new List<vm_user_day_closure_status>();
        public int TransactionCount { get; set; }
        public DateTime? FirstTransactionAt { get; set; }
        public DateTime? LastTransactionAt { get; set; }
        public bool IsBulkEntrySuspected { get; set; }
        public int BackdatedCount { get; set; }
        public double? TodayRatio { get; set; }
        public double? BaselineRatio { get; set; }
    }
}
