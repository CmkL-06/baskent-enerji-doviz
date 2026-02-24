using System.ComponentModel.DataAnnotations;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.ExchangeService.Party
{
    public class rm_partycontact
    {
        [Required]
        [StringLength(100)]
        public string ContactName { get; set; }
        
        [StringLength(100)]
        public string Position { get; set; }
        
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }
        
        [Phone]
        [StringLength(50)]
        public string Phone { get; set; }
        
        [Phone]
        [StringLength(50)]
        public string Mobile { get; set; }
        
        public bool IsPrimary { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [StringLength(500)]
        public string Notes { get; set; }
    }
}