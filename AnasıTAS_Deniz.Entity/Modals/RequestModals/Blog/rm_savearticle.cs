using AnasıTAS_Deniz.Entity.Entities.Site;
using AnasıTAS_Deniz.Entity.Entities.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.Blog
{
    public class rm_savearticle
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string SeoTitle { get; set; }
        public string? SeoLink { get; set; }
        public string? Description { get; set; }
        public string? Content { get; set; }
        public Guid AuthorId { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsUnique { get; set; }
        public bool IsOnSlider { get; set; }
        public bool IsAnnouncement { get; set; }
        public string? PosterHeaderUri { get; set; }
        public string? PosterUri { get; set; }
        public List <string>? Tags { get; set; }
        public List <Guid>? Categories { get; set; }
    }
}
