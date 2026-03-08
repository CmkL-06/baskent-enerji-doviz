using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.ResponseModals.Blog
{
    public class rsp_article_comment
    {
        public string? CreatedDate { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string FullName
        {
            get
            {
                return $"{Name} {LastName}";
            }
        }
        public string? Avatar { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        public string? IpAdress { get; set; }
        public string? Username { get; set; }
        public Guid? UserId { get; set; }
        public Guid ArticleId { get; set; }
    }

  
}
