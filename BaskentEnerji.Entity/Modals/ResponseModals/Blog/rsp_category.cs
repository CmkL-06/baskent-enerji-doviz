using BaskentEnerji.Entity.Entities;
using BaskentEnerji.Entity.Entities.Blog;
using BaskentEnerji.Entity.Entities.Site;
using BaskentEnerji.Entity.Modals.ResponseModals.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.ResponseModals.Blog
{
    public class rsp_category : BaseEntity
    {

        public string Name { get; set; }
        public string SeoTitle { get; set; }
        public string SeoLink { get; set; }
        public string? Description { get; set; }
        public string? Content { get; set; }
        public string? ImageUri { get; set; }
        public Guid? LanguageId { get; set; }
        public string? LanguageName { get; set; }
        public string? LanguageCode { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsUnique { get; set; }
        public int Order { get; set; }
        public string? PosterHeaderUri { get; set; }
        public string? PosterUri { get; set; }
        public bool IsDisplayPage { get; set; }
        public string? PageId { get; set; }
        public string? PageTitle { get; set; }

        public Guid? ParentCategoryId { get; set; }
        public rsp_category? ParentCategory { get; set; }
        public ICollection<Tag> Tags { get; set; }
    }
}
