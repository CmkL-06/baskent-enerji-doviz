using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.Site.Form
{
    public class rm_form_submit
    {
        public Guid Id { get; set; }
        public Guid FormId { get; set; }
        public string data { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
    }
}
