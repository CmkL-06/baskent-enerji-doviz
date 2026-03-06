using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Entities.Site
{
    public class BlackList : BaseEntity
    {
        public string IpAdress {  get; set; }
        public string? Reason { get; set; }
    }
}
