using MoneyTransferTurkey.Entity.Entities.Coin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.RequestModals.Coin
{
    public class rm_savecoin
    {
        public Guid Id { get; set; }
        public Guid Coin_User_TableId { get; set; }
        public Guid CoinId { get; set; }
        public Guid? UserId { get; set; }
        public string PairName { get; set; }
        public Guid PairId { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal Quantity { get; set; }
        public decimal EffQuantity { get; set; }
        public decimal? SellQuantity { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal? SellPrice { get; set; }
        public decimal FeeRate { get; set; }
    
    }
}
