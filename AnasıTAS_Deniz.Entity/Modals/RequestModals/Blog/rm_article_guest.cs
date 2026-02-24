using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.Blog
{
    public class rm_article_guest
    {
        public Guid? catId { get; set; }
        public string? relatedLink {  get; set; }
        public string? catLink {  get; set; }
        public bool? isEnabled { get; set; }
        public bool? isUnique { get; set; }
        public bool? isAnnouncement { get; set; }
        public string? Lang { get; set; }
        public bool isSimple { get; set; }
    }
}
