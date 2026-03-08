using MoneyTransferTurkey.Entity.Entities.Site;
using MoneyTransferTurkey.Entity.Entities.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Entities.Blog
{
    public class Blog_Article : BaseEntity
    {
        public string Title { get; set; }
        public string SeoTitle { get; set; }
        public string SeoLink { get; set; }
        public string? Description { get; set; }
        public string Content { get; set; }
        public User.User Author { get; set; }
        public Guid AuthorId { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsUnique { get; set; }
        public bool IsOnSlider { get; set; }
        public bool IsAnnouncement { get; set; }
        public int Views {  get; set; }
        public string? PosterHeaderUri { get; set; }
        public string? PosterUri { get; set; }

        public ICollection<Blog_Article_Tag> Blog_Article_Tags { get; set; }
        public ICollection<Blog_Article_Category> Blog_Article_Categories { get; set; }

        // public Guid CategoryId { get; set; }
        // public Blog_Category Category { get; set; }
    }
}
