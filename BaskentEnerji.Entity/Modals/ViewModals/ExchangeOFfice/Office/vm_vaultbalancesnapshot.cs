using System;
using System.Collections.Generic;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    /// <summary>
    /// View model for vault balance snapshot summary
    /// </summary>
    public class vm_vaultbalancesnapshot
    {
        public Guid Id { get; set; }
        public Guid OfficeId { get; set; }
        public string OfficeName { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public DateTime SnapshotDate { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public int TotalVaults { get; set; }
        public int TotalCurrencies { get; set; }
        public decimal TotalValueInBaseCurrency { get; set; }
        public List<vm_vaultbalancesnapshotdetail> Details { get; set; }
    }

    /// <summary>
    /// View model for individual vault balance in a snapshot
    /// </summary>
    public class vm_vaultbalancesnapshotdetail
    {
        public Guid Id { get; set; }
        public Guid SnapshotId { get; set; }
        public Guid VaultId { get; set; }
        public string VaultName { get; set; }
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        public decimal Balance { get; set; }
        public decimal ReservedAmount { get; set; }
        public decimal AvailableBalance => Balance - ReservedAmount;
        public decimal ValueInBaseCurrency { get; set; }
        public decimal ExchangeRateToBase { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
