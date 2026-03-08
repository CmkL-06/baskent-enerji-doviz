using BaskentEnerji.Entity.Entities.Site;
using BaskentEnerji.Entity.Entities.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Entities.Blog
{
    public class Blog_Category : BaseEntity
    {
       public Blog_Category? ParentCategory { get; set; }
       public Guid? ParentCategoryId { get; set; }
        public string Name { get; set; }
        public string SeoTitle { get; set; }
        public string SeoLink { get; set; }
        public string? Description { get; set; }
        public string? Content { get; set; }
        public string? ImageUri { get; set; }
        public Guid LanguageId { get; set; }
        public Language Language { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsUnique { get; set; }
        public bool IsDisplayPage { get; set; }
        public string? PageId { get; set; }
        public int Order { get; set; }
        public string? PosterHeaderUri { get; set; }
        public string? PosterUri { get; set; }

        public ICollection<Blog_Category_Tag> CategoryTags { get; set; }
        public ICollection<Blog_Article_Category> Blog_Article_Categories { get; set; }
    }
}
