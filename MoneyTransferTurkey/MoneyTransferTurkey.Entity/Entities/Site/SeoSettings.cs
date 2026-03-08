using System;

namespace MoneyTransferTurkey.Entity.Entities.Site
{
    public class SeoSettings
    {
        public int Id { get; set; }
        public Guid? PageId { get; set; } // Null for global settings
        
        // Meta tags
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public string MetaAuthor { get; set; }
        
        // Open Graph
        public string OgTitle { get; set; }
        public string OgDescription { get; set; }
        public string OgImage { get; set; }
        public string OgType { get; set; }
        
        // Twitter Card
        public string TwitterCard { get; set; }
        public string TwitterSite { get; set; }
        public string TwitterCreator { get; set; }
        
        // Schema Markup
        public string SchemaMarkup { get; set; }
        
        // Technical SEO
        public string CanonicalUrl { get; set; }
        public bool NoIndex { get; set; }
        public bool NoFollow { get; set; }
        
        // Sitemap
        public bool IncludeInSitemap { get; set; }
        public string SitemapPriority { get; set; }
        public string SitemapChangeFrequency { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation property
        public virtual Page.Page Page { get; set; }
    }
}