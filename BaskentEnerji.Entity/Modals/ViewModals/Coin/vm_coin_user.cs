using BaskentEnerji.Entity.Entities;
using BaskentEnerji.Entity.Entities.Coin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.ViewModals.Coin
{
    public class vm_coin_user : BaseEntity
    {

        public bool IsActive { get; set; }
        public string Table { get; set; }
        public Guid TableId { get; set; }
        public string? Exchange { get; set; }
        public string Name { get; set; }
        public string Pair { get; set; }
        public Guid CoinId { get; set; }
        public Guid PairId { get; set; }
        public int Order { get; set; }
        public decimal Quantity { get; set; }
        public decimal EffQuantity { get; set; } = 0;
        public decimal BuyPrice { get; set; }
        public decimal? SellPrice { get; set; }
        public decimal? FeeRate { get; set; }
        public decimal? Profit { get; set; }
        public int FixedPrice { get; set; }
        public string Icon { get; set; }
    }
}
