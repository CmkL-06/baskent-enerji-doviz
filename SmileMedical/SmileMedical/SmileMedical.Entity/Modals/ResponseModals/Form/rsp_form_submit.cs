using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Modals.ResponseModals.Form
{
    public class rsp_form_submit
    {
        public Guid Id { get; set; }
        public Guid FormId { get; set; }
        public string FormName { get; set; }
        public string data { get; set; }
        public string CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
    }
}
