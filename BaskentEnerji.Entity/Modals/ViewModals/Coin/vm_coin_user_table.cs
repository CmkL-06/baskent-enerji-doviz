using BaskentEnerji.Entity.Entities;
using BaskentEnerji.Entity.Entities.Coin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.ViewModals.Coin
{
    public class vm_coin_user_table  :BaseEntity
    {
        public string Name { get; set; }
        public int Order { get; set; }

        public string? Username { get; set; }
        //public decimal? Profit { get; set; }

        public List<vm_coin_user>? Coins { get; set; }
        public vm_coin_profit_stats ProfitStat { get; set; }
    }
}
