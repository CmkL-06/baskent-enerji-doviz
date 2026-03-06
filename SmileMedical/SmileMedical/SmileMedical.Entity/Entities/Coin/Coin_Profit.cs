using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Entities.Coin
{
    public class Coin_Profit : BaseEntity
    {
        public Guid UserId { get; set; }
        public User.User User { get; set; }

     

        public Guid CoinId { get; set; }
        public Coin Coin { get; set; }

        public Guid PairId { get; set; }
        public Coin_Pair Pair { get; set; }
     

      
        public decimal Quantity { get; set; }
        public decimal EffQuantity { get; set; } = 0;
        public decimal BuyPrice { get; set; }
        public decimal? SellPrice { get; set; }
        public decimal FeeRate { get; set; }

        public decimal? Profit { get; set; }
    }
}
