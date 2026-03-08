using System.ComponentModel.DataAnnotations;

namespace MoneyTransferTurkey.Entity.Modals.RequestModals.Site.Page
{
    public class DuplicatePageRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(200)]
        public string NewSlug { get; set; }
        
        [Required]
        [MinLength(1)]
        [MaxLength(200)]
        public string NewTitle { get; set; }
        
        [MinLength(2)]
        [MaxLength(5)]
        public string? LanguageCode { get; set; }
    }
}