using System;
using System.ComponentModel.DataAnnotations;

namespace MoneyTransferTurkey.Entity.Entities.Site
{
    public class GlobalColor : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "Default Theme";

        [Required]
        public string ColorsJson { get; set; } = "{}";

        public bool IsActive { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}