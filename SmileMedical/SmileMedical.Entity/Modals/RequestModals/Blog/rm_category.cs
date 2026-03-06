using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Modals.RequestModals.Blog
{
    public class rm_category
    {
        public string? search { get; set; }
        public bool? isMain { get; set; }
        public Guid? langId { get; set; }
        public Guid? ArticleId { get; set; }
        public Guid? catId { get; set; }
        public bool? isEnabled { get; set; }
        public bool? isUnique { get; set; }
        public bool? withoutDescription { get; set; }
    }
}
