using MoneyTransferTurkey.Entity.Entities.Blog;
using MoneyTransferTurkey.Entity.Entities.Site;
using System;
using System.Collections.Generic;

namespace MoneyTransferTurkey.Entity.Entities.Site.Menu
{
    public class Menu : BaseEntity
    {
        public string Name { get; set; }
        public int Order { get; set; }
        public Guid LanguageId { get; set; }
        public string? CustomUrl { get; set; }
        public Language Language { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsOnNavbar { get; set; }
        public bool IsOnAside { get; set; }
        public bool IsOnFooter { get; set; }
        public bool IsForMobile { get; set; }
        public bool IsCategoryMenu { get; set; }
        
 
        public Guid? Blog_CategoryId { get; set; }
        public Blog_Category Blog_Category { get; set; }
        public Guid? ParentMenuId { get; set; }
        public Menu ParentMenu { get; set; }
        public ICollection<Menu> SubMenus { get; set; } = new List<Menu>();
        public ICollection<Menu_Item> MenuItems { get; set; } = new List<Menu_Item>();
    }
}
