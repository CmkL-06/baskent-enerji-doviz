using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.Site.Tag
{
    public class rm_addtag_article
    {
        public Guid TagId { get; set; }
        public Guid ArticleId { get; set; }
    }
}
