using AnasıTAS_Deniz.Entity.Modals.RequestModals.Site.Slider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Infrastructure.Site.Slider
{
    public interface ISliderServiceCommand
    {
        Task SaveSlider(rm_saveslider data);
        Task DeleteSlider(Guid sliderId);

        Task SaveSliderItem(rm_saveslideritem data);
        Task DeleteSliderItem(Guid itemId);
    }
}
