using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Entities.Coin
{
    public class Coin : BaseEntity
    {
        public string Name { get; set; }
        public string? Icon { get; set; }
        public string? Cover { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public int FixedPrice { get; set; }
        public bool IsActive { get; set; }
        public string? Exchange{ get; set; }

        public ICollection<Coin_Pair> Pairs { get; set; } = new List<Coin_Pair>();

    }
}
