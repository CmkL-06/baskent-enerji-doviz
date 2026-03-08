using System;
using System.Collections.Generic;

namespace MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_vaultcount
    {
        public Guid Id { get; set; }
        public Guid VaultId { get; set; }
        public string VaultName { get; set; }
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public DateTime CountDate { get; set; }
        public bool HasDiscrepancy { get; set; }
        public string DiscrepancyDetails { get; set; }
        public bool IsSystemGenerated { get; set; }
        public List<vm_vaultcountdetail> CountDetails { get; set; }
    }

    public class vm_vaultcountdetail
    {
        public Guid Id { get; set; }
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal SystemAmount { get; set; }
        public decimal Discrepancy { get; set; }
    }
}