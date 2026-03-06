using System;
using System.Collections.Generic;

namespace SmileMedical.Entity.Entities.Site.Page
{
    public class Page : SmileMedical.Entity.Entities.BaseEntity
    {
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Keywords { get; set; }
        public string LanguageCode { get; set; } = "en";
        public bool IsHomePage { get; set; }
        public PageStatus Status { get; set; } = PageStatus.Draft;
        
        // Components are now stored as a nested tree structure
        public List<BuilderComponent> Components { get; set; } = new List<BuilderComponent>();
        
        // Page-level custom CSS and JavaScript
        public string? CustomCSS { get; set; }
        public string? CustomJS { get; set; }
        
        // Timestamps
        public DateTime? PublishedAt { get; set; }
        
        // Relations
        public Guid? LanguageId { get; set; }
        public Site.Language? Language { get; set; }
        
        // SEO
        public string? OgImage { get; set; }
        public string? OgTitle { get; set; }
        public string? OgDescription { get; set; }
        
        // Floating Contact Settings (stored as JSON)
        public string? FloatingContactJson { get; set; }
        
        // Page Background Color
        public string? BackgroundColor { get; set; }
    }

    public enum PageStatus
    {
        Draft,
        Published,
        Archived
    }
}