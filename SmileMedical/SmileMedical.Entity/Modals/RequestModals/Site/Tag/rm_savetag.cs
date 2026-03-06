using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Modals.RequestModals.Site.Tag
{
    public class rm_savetag
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string SeoTitle { get; set; }
        public string Content { get; set; }
        public string SeoDescription { get; set; }
        public Guid LanguageId { get; set; }
    }
}
