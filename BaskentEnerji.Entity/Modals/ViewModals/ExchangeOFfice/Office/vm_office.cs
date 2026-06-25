using BaskentEnerji.Entity.Entities;
using System;
using System.Collections.Generic;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_office : BaseEntity
    {
        public string OfficeName { get; set; }
        public string? OfficeDescription { get; set; }
        public string? OfficeImageUri { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }

        // Hiyerarşi
        public string OfficeType { get; set; }
        public Guid? ParentOfficeId { get; set; }
        public string? ParentOfficeName { get; set; }

        // Bayi alanları
        public decimal? DailyTransactionLimit { get; set; }
        public decimal? MonthlyTransactionLimit { get; set; }
        public decimal? CommissionRate { get; set; }

        // İstatistikler (opsiyonel, özet endpoint için)
        public int VaultCount { get; set; }
        public int UserCount { get; set; }
        public List<vm_office>? Children { get; set; }
    }
}
