using MoneyTransferTurkey.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.ViewModals.Slider
{
    public class vm_slider_guest
    {
        public string Name { get; set; }
        public string CreatedDate { get; set; }
        public List<vm_slideritem_guest> SliderItems { get; set; }
    }
}
