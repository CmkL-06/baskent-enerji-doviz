using MoneyTransferTurkey.Entity.Entities.Blog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Entities.Site.Menu
{
    public class Menu_Item : BaseEntity
    {
        public string Name { get; set; }
        public string SeoTitle { get; set; }
        public string SeoLink { get; set; }
        public int Order { get; set; } = 0;
        public bool IsEnabled { get; set; }
        public Guid MenuId { get; set; }
        public Menu Menu { get; set; }
        public bool IsSingleCategoryItem { get; set; }
        public bool IsMainCategoryItem { get; set; }
        public bool IsCustomLink { get; set; }
        public bool IsPage {  get; set; }
        public Guid? MainCategoryId { get; set; }
        public Blog_Category MainCategory { get; set; }
        public Guid? Blog_CategoryId { get; set; }
        public Blog_Category Blog_Category { get; set; }
    }
}
