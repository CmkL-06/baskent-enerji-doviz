using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.RequestModals.Blog
{
    public class rm_category_guest
    {
        public bool isSimple { get; set; }
        public string? link { get; set; }
        public string? parentLink { get; set; }
        public Guid? langId { get; set; }
        public Guid? ArticleId { get; set; }
        public Guid? catId { get; set; }
        public bool? isEnabled { get; set; }
        public bool? isUnique { get; set; }
    }
}
