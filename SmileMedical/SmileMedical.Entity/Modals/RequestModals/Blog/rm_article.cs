using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Modals.RequestModals.Blog
{
    public class rm_article
    {
        public Guid? catId { get; set; }
        public string? search { get; set; }
        public bool? isEnabled { get; set; }
        public bool? isUnique { get; set; }
        public bool? isAnnouncement { get; set; }
        public bool isSimple { get; set; }
    }
}
