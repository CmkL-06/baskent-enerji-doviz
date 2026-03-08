using MoneyTransferTurkey.Entity.Modals.ResponseModals.Blog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.RequestModals.Site.Menu
{
    public class rm_menu_guest
    {
        public bool? IsMain { get; set; }
        public bool? IsNavbar { get; set; }
        public bool? IsFooter { get; set; }
        public bool? IsMobile { get; set; }
        public bool? IsCategoryMenu { get; set; }
        public Guid? ParentId { get; set; }
        public Guid? LanguageId { get; set; }
        public string? Lang { get; set; }

        
    }
}
