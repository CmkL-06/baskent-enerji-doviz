using AnasıTAS_Deniz.Entity.Entities.Blog;
using AnasıTAS_Deniz.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.Blog
{
    public class rm_savecategory
    {
        public Guid? Id { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public string Name { get; set; }
        public string SeoTitle { get; set; }
        public string SeoLink { get; set; }
        public string Content { get; set; }
        public string? Description { get; set; }
        public string? ImageUri { get; set; }
        public Guid LanguageId { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsUnique { get; set; }
        public int Order { get; set; }
        public string? PosterHeaderUri { get; set; }
        public string? PosterUri { get; set; }
        public string? PageId { get; set; }
        public bool IsDisplayPage => !string.IsNullOrEmpty(PageId);
        public List<string> Tags { get; set; }
    }
}
