using AnasıTAS_Deniz.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.Site.Slider
{
    public class rm_saveslider
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public Guid LanguageId { get; set; }
        public bool IsHome { get; set; } = false;
        public bool IsBlog { get; set; } = false;
        public bool IsEnabled { get; set; }
    }
}
