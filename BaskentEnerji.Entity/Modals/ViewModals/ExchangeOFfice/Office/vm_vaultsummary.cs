using BaskentEnerji.Entity.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_vaultsummary : BaseEntity
    {
        public Guid VaultId { get; set; }
        public string VaultName { get; set; }
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; }
        public bool IsActive { get; set; }
        public bool ShouldCount { get; set; }
        public DateTime? LastCountDate { get; set; }
        public List<vm_vaultbalance> Balances { get; set; }
        public List<vm_vaultbalancehistory> BalanceHistories { get; set; }
        public decimal TotalValueInBaseCurrency { get; set; }
    }
}
