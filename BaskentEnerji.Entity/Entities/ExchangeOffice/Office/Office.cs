using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Office
{
    public class Office : BaseEntity
    {
        public string OfficeName { get; set; }
        public string? OfficeDescription { get; set; }
        public string? OfficeImageUri { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;

        // Hiyerarşi
        public OfficeType OfficeType { get; set; } = OfficeType.Sube;
        public Guid? ParentOfficeId { get; set; }
        public Office? ParentOffice { get; set; }
        public ICollection<Office>? ChildOffices { get; set; }

        // Bayi limitleri ve komisyon
        public decimal? DailyTransactionLimit { get; set; }
        public decimal? MonthlyTransactionLimit { get; set; }
        public decimal? CommissionRate { get; set; }

        // Kur yönetimi
        public RateInheritanceMode RateInheritanceMode { get; set; } = RateInheritanceMode.UseParent;

        // Transfer onay eşiği (bu tutarın altındaki transferler otomatik onaylanır)
        public decimal? TransferApprovalThreshold { get; set; }

        // Navigation
        public ICollection<Vault> Vaults { get; set; }
        public ICollection<User.User> Employees { get; set; }
    }
}
