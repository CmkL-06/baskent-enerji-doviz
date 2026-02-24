using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.Coin
{
    public class rm_coin_get
    {
        public Guid? coinId {  get; set; }
        public string? coinName {  get; set; }
        public string? search {  get; set; }
    }
}
