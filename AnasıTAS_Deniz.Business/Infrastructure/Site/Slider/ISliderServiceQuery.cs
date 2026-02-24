using AnasıTAS_Deniz.Entity.Modals.RequestModals.Site.Slider;
using AnasıTAS_Deniz.Entity.Modals.ViewModals.Slider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Infrastructure.Site.Slider
{
    public interface ISliderServiceQuery
    {
        Task<List<vm_slider>> GetSliders(rm_slider data);
        Task<List<vm_slider_guest>> GetSliders_Guest(rm_slider data);
    }
}
