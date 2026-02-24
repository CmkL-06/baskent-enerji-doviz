using AnasıTAS_Deniz.Entity.Entities.Blog;
using AnasıTAS_Deniz.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.Site.Menu
{
    public class rm_menu_save
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? CustomUrl { get; set; }
        public int Order { get; set; }
        public Guid LanguageId { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsOnNavbar { get; set; }
        public bool IsOnAside { get; set; }
        public bool IsOnFooter { get; set; }
        public bool IsForMobile { get; set; }
        public bool IsCategoryMenu { get; set; }
        public Guid? Blog_CategoryId { get; set; }
        public Guid? ParentMenuId { get; set; }
    }
}
