using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Modals.RequestModals.Coin
{
    public class rm_savetable
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        public bool delete { get; set; }

    }
}
