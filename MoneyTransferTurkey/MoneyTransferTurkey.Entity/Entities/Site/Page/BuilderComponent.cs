using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MoneyTransferTurkey.Entity.Entities.Site.Page
{
    public class BuilderComponent
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        
        // Properties stored as JSON for flexibility
        public string PropsJson { get; set; } = "{}";
        
        // Styles for different breakpoints
        public string? DesktopStyles { get; set; }
        public string? TabletStyles { get; set; }
        public string? MobileStyles { get; set; }
        public string? CustomCSS { get; set; }
        
        // Component state
        public bool Locked { get; set; } = false;
        public bool Hidden { get; set; } = false;
        
        // Parent-child relationship for nested components
        public Guid? ParentId { get; set; }
        public BuilderComponent? Parent { get; set; }
        public List<BuilderComponent> Children { get; set; } = new List<BuilderComponent>();
        
        // Page reference
        public Guid PageId { get; set; }
        public Page Page { get; set; }
        
        // Helper property to parse props
        [NotMapped]
        public Dictionary<string, object> Props
        {
            get
            {
                try
                {
                    return string.IsNullOrEmpty(PropsJson)
                        ? new Dictionary<string, object>()
                        : JsonSerializer.Deserialize<Dictionary<string, object>>(PropsJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                }
                catch
                {
                    return new Dictionary<string, object>();
                }
            }
            set => PropsJson = JsonSerializer.Serialize(value);
        }
        
        // Combined styles for rendering
        [NotMapped]
        public ComponentStyles Styles => new ComponentStyles
        {
            Desktop = DesktopStyles,
            Tablet = TabletStyles,
            Mobile = MobileStyles,
            CustomCSS = CustomCSS
        };
    }
    
    public class ComponentStyles
    {
        public string? Desktop { get; set; }
        public string? Tablet { get; set; }
        public string? Mobile { get; set; }
        public string? CustomCSS { get; set; }
    }
}