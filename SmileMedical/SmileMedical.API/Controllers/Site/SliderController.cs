using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmileMedical.Business.Infrastructure.Site.Slider;
using SmileMedical.Entity.Modals.RequestModals.Site.Slider;
using SmileMedical.Entity.Modals.ViewModals.Slider;

namespace SmileMedical.API.Controllers.Site
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class SliderController : ControllerBase
    {
        private readonly ISliderServiceCommand _sliderServiceCommand;
        private readonly ISliderServiceQuery _sliderServiceQuery;

        public SliderController(ISliderServiceCommand sliderServiceCommand, ISliderServiceQuery sliderServiceQuery)
        {
            _sliderServiceCommand = sliderServiceCommand;
            _sliderServiceQuery = sliderServiceQuery;
        }

        [HttpPost]
        public async Task SaveSlider(rm_saveslider data)
        {
            await _sliderServiceCommand.SaveSlider(data);
        }

        [HttpPost("delete")]
        public async Task DeleteSlider([FromBody]Guid sliderId)
        {
            await _sliderServiceCommand.DeleteSlider(sliderId);
        }

        [HttpPost("item")]
        public async Task SaveSliderItem(rm_saveslideritem data)
        {
            await _sliderServiceCommand.SaveSliderItem(data);
        }
        [HttpPost("item/delete")]
        public async Task DeleteSliderItem([FromBody] Guid itemId)
        {
            await _sliderServiceCommand.DeleteSliderItem(itemId);
        }

        [HttpGet("admin")]
        public async Task<List<vm_slider>> GetSliders([FromQuery] rm_slider data)
        {
          return  await _sliderServiceQuery.GetSliders(data);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<List<vm_slider_guest>> GetSliders_Guest([FromQuery]rm_slider data)
        {
          return   await _sliderServiceQuery.GetSliders_Guest(data);
        }
    }
}
