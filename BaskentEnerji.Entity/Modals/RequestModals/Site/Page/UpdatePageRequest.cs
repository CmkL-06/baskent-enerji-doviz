using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BaskentEnerji.Entity.Entities.Site.Page;

namespace BaskentEnerji.Entity.Modals.RequestModals.Site.Page
{
    public class UpdatePageRequest
    {
        [MinLength(1)]
        [MaxLength(200)]
        public string? Slug { get; set; }
        
        [MinLength(1)]
        [MaxLength(200)]
        public string? Title { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [MaxLength(500)]
        public string? Keywords { get; set; }
        
        [MinLength(2)]
        [MaxLength(5)]
        public string? LanguageCode { get; set; }
        
        public bool? IsHomePage { get; set; }
        
        public PageStatus? Status { get; set; }
        
        public List<BuilderComponentDto>? Components { get; set; }
        
        public string? CustomCSS { get; set; }
        
        public string? CustomJS { get; set; }
        
        // SEO
        public string? OgImage { get; set; }
        public string? OgTitle { get; set; }
        public string? OgDescription { get; set; }
        
        // Floating Contact Settings
        public FloatingContactDto? FloatingContact { get; set; }
        
        // Page Background Color
        public string? BackgroundColor { get; set; }
    }
}