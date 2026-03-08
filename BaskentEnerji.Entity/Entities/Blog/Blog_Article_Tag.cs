using BaskentEnerji.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Entities.Blog
{
    public class Blog_Article_Tag : BaseEntity
    {
     
        public Guid TagId { get; set; }
        public Tag Tag { get; set; }
        public Guid ArticleId { get; set; }
        public Blog_Article Article { get; set; }
   
    }
}
