using System;

namespace BaskentEnerji.Entity.Entities.Site
{
    public class Analytics
    {
        public int Id { get; set; }
        public Guid? PageId { get; set; }
        public string PageSlug { get; set; }
        public string SessionId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string Referrer { get; set; }
        public string Device { get; set; } // Desktop, Mobile, Tablet
        public string Browser { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public DateTime VisitDate { get; set; }
        public int Duration { get; set; } // Session duration in seconds
        public bool IsBounce { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Navigation property
        public virtual Page.Page Page { get; set; }
    }
}