using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Entities.Site
{
    public class Site_Settings :BaseEntity
    {
        public string? SiteName { get; set; }
        public string? SiteUrl { get; set; }
        public string? LogoUrl { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? FacebookUrl { get; set; }
        public string? TwitterUrl { get; set; } 
        public string? InstagramUrl { get; set; }
        public string? LinkedinUrl { get; set; }

    }
}
