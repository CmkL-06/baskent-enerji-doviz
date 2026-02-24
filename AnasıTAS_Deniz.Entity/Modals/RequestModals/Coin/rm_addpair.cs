using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.Coin
{
    public class rm_addpair
    {
        public Guid SourceCoinId { get; set; }
        public string Pair { get; set; }
    }
}
