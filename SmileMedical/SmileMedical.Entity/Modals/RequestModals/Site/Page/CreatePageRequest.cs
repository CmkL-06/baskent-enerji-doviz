using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SmileMedical.Entity.Entities.Site.Page;

namespace SmileMedical.Entity.Modals.RequestModals.Site.Page
{
    public class CreatePageRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(200)]
        public string Slug { get; set; }
        
        [Required]
        [MinLength(1)]
        [MaxLength(200)]
        public string Title { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [MaxLength(500)]
        public string? Keywords { get; set; }
        
        [Required]
        [MinLength(2)]
        [MaxLength(5)]
        public string LanguageCode { get; set; }
        
        public bool IsHomePage { get; set; } = false;
        
        public PageStatus Status { get; set; } = PageStatus.Draft;
        
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
    
    public class FloatingContactDto
    {
        public bool Enabled { get; set; }
        public List<ContactButtonDto> Contacts { get; set; } = new List<ContactButtonDto>();
        public string? DesktopPosition { get; set; }
        public string? MobilePosition { get; set; }
        public string? MobileFloatingPosition { get; set; }
    }
    
    public class ContactButtonDto
    {
        public string Icon { get; set; }
        public string Link { get; set; }
        public string? Label { get; set; }
        public string? BackgroundColor { get; set; }
        public string? IconColor { get; set; }
        public string? LabelColor { get; set; }
        public string? Target { get; set; }
    }
    
    public class BuilderComponentDto
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string? PropsJson { get; set; }  // Added to accept JSON string from frontend
        public Dictionary<string, object> Props { get; set; } = new Dictionary<string, object>();
        public ComponentStylesDto Styles { get; set; } = new ComponentStylesDto();
        public List<BuilderComponentDto>? Children { get; set; }
        public int Order { get; set; }
        public bool Locked { get; set; } = false;
        public bool Hidden { get; set; } = false;
    }
    
    public class ComponentStylesDto
    {
        public string? Desktop { get; set; }
        public string? Tablet { get; set; }
        public string? Mobile { get; set; }
        public string? CustomCSS { get; set; }
    }
}