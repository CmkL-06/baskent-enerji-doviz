using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Entities.Coin
{
    public class Coin_Pair : BaseEntity
    {
        public Guid SourceCoinId { get; set; }
        public string Pair { get; set; }

    }
}
