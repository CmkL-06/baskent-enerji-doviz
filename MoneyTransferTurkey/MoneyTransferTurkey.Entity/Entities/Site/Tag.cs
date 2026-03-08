using MoneyTransferTurkey.Entity.Entities.Blog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Entities.Site
{
    public class Tag : BaseEntity
    {
        public string Name { get; set; }
        public string SeoTitle { get; set; }
        public string SeoLink { get; set; }
        public string Content { get; set; }
        public Guid LanguageId { get; set; }

        [JsonIgnore]
        public Language Language { get; set; }

        [JsonIgnore]
        public ICollection<Blog_Article_Tag> ArticleTags { get; set; }
        [JsonIgnore]
        public ICollection<Blog_Category_Tag> CategoryTags { get; set; }
    }
}
