using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Entities.Site
{
    public class Theme :BaseEntity
    {
       
       
        [MaxLength(100)]
        public string Name { get; set; }
        
        public string? Colors { get; set; } // Stored as JSON
        public bool IsDefault { get; set; }
        public bool IsDark { get; set; }
    }
}
