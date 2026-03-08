using BaskentEnerji.Entity.Entities;
using BaskentEnerji.Entity.Entities.Site;
using BaskentEnerji.Entity.Modals.ResponseModals.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.ResponseModals.Blog
{
    public class rsp_category_guest :BaseEntity
    {

     
        public string Name { get; set; }
        public string? ParentCategoryName { get; set; }
        public string SeoTitle { get; set; }
        public string SeoLink { get; set; }
        public string? Content { get; set; }
        public string? Description { get; set; }
        public string? ImageUri { get; set; }
        public string? Language { get; set; }
        public string? PosterHeaderUri { get; set; }
        public string? PosterUri { get; set; }
        public string? PageId { get; set; }
        public string? Order { get; set; }
        public bool IsDisplayPage { get; set; }
        public List<rsp_tag_guest> Tags { get; set; }
    }
}
