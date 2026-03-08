using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Entities.Blog
{
    public class Blog_Article_Visit : BaseEntity
    {
        public string VisitorIp { get; set; }
        public Guid ArticleId { get; set; }
        public Blog_Article Article { get; set; }
    }
}
