using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office
{
    public class rm_saveoffice
    {
        public Guid Id { get; set; }
        public string OfficeName { get; set; }
        public string? OfficeDescription { get; set; }
        public string? OfficeImageUri { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }

        // Hiyerarşi
        public OfficeType OfficeType { get; set; } = OfficeType.Sube;
        public Guid? ParentOfficeId { get; set; }

        // Bayi alanları (OfficeType == Bayi ise kullanılır)
        public decimal? DailyTransactionLimit { get; set; }
        public decimal? MonthlyTransactionLimit { get; set; }
        public decimal? CommissionRate { get; set; }
    }
}
