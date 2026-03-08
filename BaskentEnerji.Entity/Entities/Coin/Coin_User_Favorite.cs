using BaskentEnerji.Entity.Entities.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Entities.Coin
{
    public class Coin_User_Favorite : BaseEntity
    {
        public Guid UserId { get; set; }
        public User.User User { get; set; }
        public Guid CoinId { get; set; }
        public Coin Coin { get; set; }
        public string PairName { get; set; }
        public int Order { get; set; }
    }
}
