using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.RequestModals.Coin
{
    public class rm_savecoin_admin
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Exchange { get; set; }
        public string? Icon { get; set; }
        public string? Cover { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public int FixedPrice { get; set; }
        public List<string>? Pairs { get; set; }
        public bool IsActive { get; set; }
    }
}
