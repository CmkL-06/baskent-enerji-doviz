using AnasıTAS_Deniz.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Entities.Blog
{
    public class Blog_Category_Tag : BaseEntity
    {
        public Guid TagId { get; set; }
        public Tag Tag { get; set; }
        public Guid CategoryId { get; set; }
        public Blog_Category Category { get; set; }
    }
}
