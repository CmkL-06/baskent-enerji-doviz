using BaskentEnerji.Entity.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_vaultbalance : BaseEntity
    {
        public Guid CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
       // string? Description { get; set; }
        public decimal Balance { get; set; }
       // public decimal ReservedAmount { get; set; }
        //public decimal AvailableBalance => Balance - ReservedAmount;
        public decimal AvailableBalance => Balance;
        public decimal ValueInBaseCurrency { get; set; }
        public decimal ExchangeRateToBase { get; set; }
    }
}
