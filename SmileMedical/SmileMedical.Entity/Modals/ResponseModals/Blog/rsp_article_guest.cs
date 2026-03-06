using SmileMedical.Entity.Entities;
using SmileMedical.Entity.Modals.ResponseModals.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Modals.ResponseModals.Blog
{
    public class rsp_article_guest : BaseEntity
    {
        public string Title { get; set; }
        public string SeoTitle { get; set; }
        public string SeoLink { get; set; }
        public string? Description { get; set; }
        public string Content { get; set; }
        public string? Author { get; set; }
        public int Views { get; set; }
        public bool IsAnnouncement { get; set; }
        public string? PosterHeaderUri { get; set; }
        public string? PosterUri { get; set; }

        public List<rsp_article_comment>? Comments { get; set; }
        public List<rsp_category_guest>? Categories { get; set; }
        public List<rsp_tag_guest>? Tags { get; set; }
    }
}
