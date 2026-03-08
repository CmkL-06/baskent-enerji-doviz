using BaskentEnerji.Entity.Entities.Blog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.RequestModals.Site.Menu
{
    public class rm_menuitem_save
    {
        public Guid? Id { get; set; } 
        public string Name { get; set; }
        public int Order { get; set; }
        public string SeoTitle { get; set; } = string.Empty;
        public string? SeoLink { get; set; } = string.Empty;
        //public string? CustomLink { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public Guid MenuId { get; set; }
        public bool IsSingleCategoryItem { get; set; }
        public bool IsMainCategoryItem { get; set; }
        public bool IsCustomLink { get; set; }
        public Guid? MainCategoryId { get; set; }
        public Guid? Blog_CategoryId { get; set; }
    }
}
