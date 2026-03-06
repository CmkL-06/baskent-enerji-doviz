using SmileMedical.Entity.Entities.Blog;
using SmileMedical.Entity.Entities.Site.Menu;
using SmileMedical.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmileMedical.Entity.Entities;

namespace SmileMedical.Entity.Modals.ViewModals.Menu
{
    public class vm_menu_guest  : BaseEntity
    {
        public string Name { get; set; }
        public string? CustomUrl { get; set; }
        public int Order { get; set; }
        //public bool HasSubMenus { get; set; }
        public Guid? LanguageId { get; set; }
        public string? LanguageCode { get; set; }
        public string? LanguageName { get; set; }
        public ICollection<vm_menu_guest> SubMenus { get; set; } = new List<vm_menu_guest>();
        public ICollection<vm_menu_item_guest> MenuItems { get; set; } = new List<vm_menu_item_guest>();
    }
}
