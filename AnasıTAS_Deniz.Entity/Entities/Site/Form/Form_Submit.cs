using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Entities.Site.Form
{
    public class Form_Submit : BaseEntity
    {
        public Guid FormId { get; set; }
        public Form Form { get; set; }
        public string data { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public string? IpAdress { get; set; }
    }
}
