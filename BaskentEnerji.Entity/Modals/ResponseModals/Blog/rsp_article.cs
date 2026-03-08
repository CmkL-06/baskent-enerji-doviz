using BaskentEnerji.Entity.Entities;
using BaskentEnerji.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.ResponseModals.Blog
{
    public class rsp_article : BaseEntity
    {
        public string Title { get; set; }
        public string SeoTitle { get; set; }
        public string SeoLink { get; set; }
        public string? Description { get; set; }
        public string Content { get; set; }
        public Guid AuthorId { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsUnique { get; set; }
        public bool IsOnSlider { get; set; }
        public bool IsAnnouncement { get; set; }
        public string? PosterHeaderUri { get; set; }
        public string? PosterUri { get; set; }
        public List<rsp_article_comment> Comments {get;set;}
        public List <rsp_category> Categories { get; set; }
        public List<Tag> Tags { get; set; }
     
    }
}
