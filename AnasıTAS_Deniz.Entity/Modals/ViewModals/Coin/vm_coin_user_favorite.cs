using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.ViewModals.Coin
{
    public class vm_coin_user_favorite
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Pair { get; set; }
        public Guid CoinId { get; set; }
        public int Order { get; set; }
        public int FixedPrice { get; set; }
        public string Icon { get; set; }
        public string Exchange { get; set; }
    }
}
