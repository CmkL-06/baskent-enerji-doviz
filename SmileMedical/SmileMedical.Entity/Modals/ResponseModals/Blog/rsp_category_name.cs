using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Modals.ResponseModals.Blog
{
    public class rsp_category_name
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string SeoTitle { get; set; }
        public string SeoLink { get; set; }
        public string? PosterHeaderUri { get; set; }
        public string? PosterUri { get; set; }
        public bool IsDisplayPage { get; set; }
        public string? PageId { get; set; }
    }
}
