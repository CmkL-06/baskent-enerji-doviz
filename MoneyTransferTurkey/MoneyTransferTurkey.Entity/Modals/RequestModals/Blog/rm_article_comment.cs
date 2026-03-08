using MoneyTransferTurkey.Entity.Entities.Blog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.RequestModals.Blog
{
    public class rm_article_comment
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        public string? IpAdress { get; set; }
        public Guid? UserId { get; set; }
        public Guid? ArticleId { get; set; }
        public string? ArticleLink { get; set; }

    }

 
}
