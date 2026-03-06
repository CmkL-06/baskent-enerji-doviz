using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Entities.Blog
{
    public class Blog_Article_Category : BaseEntity
    {
        public Blog_Article Article { get; set; }
        public Guid ArticleId { get; set; }

        public Blog_Category Category { get; set; } 
        public Guid CategoryId { get; set; }

      
    }
}
