using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Modals.RequestModals.Coin
{
    public class rm_savefavoritecoin
    {
        public Guid Id { get; set; }
        public Guid CoinId { get; set; }
        public string PairName { get; set; }
        public int Order { get; set; }
       
    }
}
