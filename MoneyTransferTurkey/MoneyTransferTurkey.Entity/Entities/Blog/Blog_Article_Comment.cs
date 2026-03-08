using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Entities.Blog
{
    public class Blog_Article_Comment : BaseEntity
    {

        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        public string? IpAdress { get; set; }
        public Guid? UserId { get; set; }
        public Entity.Entities.User.User? User { get; set; }
        public Guid ArticleId { get; set; }
        public Blog_Article Article { get; set; }
    }
}
