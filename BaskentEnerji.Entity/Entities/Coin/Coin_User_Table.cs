using BaskentEnerji.Entity.Modals.ViewModals.Coin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Entities.Coin
{
    public class Coin_User_Table : BaseEntity
    {
        public string Name { get; set; }
        public int Order { get; set; }

        public Guid UserId { get; set; }
        public User.User User { get; set; }

        public List<Coin_User> Coins { get; set; }
    
    }
}
