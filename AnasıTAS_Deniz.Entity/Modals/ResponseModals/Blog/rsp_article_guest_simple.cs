using AnasıTAS_Deniz.Entity.Modals.ResponseModals.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.ResponseModals.Blog
{
    public class rsp_article_guest_simple
    {
        public string Title { get; set; }
        public string SeoTitle { get; set; }
        public string SeoLink { get; set; }
        public int Views { get; set; }
        public bool IsAnnouncement { get; set; }
        public string? PosterHeaderUri { get; set; }
        public string? PosterUri { get; set; }
        public List<rsp_category_guest>? Categories { get; set; }
        public List<rsp_tag_guest>? Tags { get; set; }
    }
}
