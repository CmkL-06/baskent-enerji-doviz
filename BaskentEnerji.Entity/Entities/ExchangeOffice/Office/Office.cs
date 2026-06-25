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

        // Navigation
        public ICollection<Vault> Vaults { get; set; }
        public ICollection<User.User> Employees { get; set; }
        public ICollection<OfficeTransfer> OutgoingTransfers { get; set; }
        public ICollection<OfficeTransfer> IncomingTransfers { get; set; }
    }
}
