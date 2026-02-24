using AnasıTAS_Deniz.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.ViewModals.Slider
{
    public class vm_slider
    {
        public string CreatedDate { get; set; }
        public string Name { get; set; }
        public Guid LanguageId { get; set; }
        public Language Language { get; set; }
        public bool IsHome { get; set; } = false;
        public bool IsBlog { get; set; } = false;
        public bool IsEnabled { get; set; }
        ICollection<vm_slideritem> SliderItems { get; set; }
    }
}
