using MoneyTransferTurkey.Entity.Entities;
using MoneyTransferTurkey.Entity.Entities.Blog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.ViewModals.Menu
{
    public class vm_menu_item :BaseEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string SeoTitle { get; set; }
        public string SeoLink { get; set; }
        public int Order { get; set; }
        public bool IsEnabled { get; set; }
        public Guid MenuId { get; set; }
        public String MenuName { get; set; } // *
        public bool IsSingleCategoryItem { get; set; }
        public bool IsMainCategoryItem { get; set; }
        public bool IsCustomLink { get; set; }
        public Guid? MainCategoryId { get; set; }
        public Guid? Blog_CategoryId { get; set; }

    }
}
