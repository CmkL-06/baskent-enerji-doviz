using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.RequestModals.Blog
{
    public class rm_article_addcategory
    {
        public Guid ArticleId { get; set; }
        public Guid CategoryId { get; set; }
    }
}
