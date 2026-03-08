using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.RequestModals.Site.Slider
{
    public class rm_slider
    {
        public Guid? Id { get; set; }
        public bool? IsEnabled { get; set; }
        public string? LanguageCode { get; set; } 
        public Guid? LanguageId { get; set; }
        public bool? IsHome { get; set; } 
        public bool? IsBlog { get; set; } 
      
    }
}
