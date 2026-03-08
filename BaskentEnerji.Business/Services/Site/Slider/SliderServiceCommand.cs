using AutoMapper;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.Site.Slider;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.Site.Menu;
using BaskentEnerji.Entity.Entities.Site.Slider;
using BaskentEnerji.Entity.Modals.RequestModals.Site.Slider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.Site.Slider
{
    public class SliderServiceCommand : ISliderServiceCommand
    {
        private readonly BaskentEnerjiDbContext _dbContext;
        private readonly ValidationService _validationService;
        private readonly IMapper mapper;

        public SliderServiceCommand(BaskentEnerjiDbContext dbContext, ValidationService validationService, IMapper mapper)
        {
            _dbContext = dbContext;
            _validationService = validationService;
            this.mapper = mapper;
        }

        public async Task SaveSlider(rm_saveslider data)
        {
            if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
            if (data.Name == null) throw new ApiException(HttpStatusCode.NotAcceptable, "Please define a slider name");
            var dbSlider = await _dbContext.Sliders.FindAsync(data.Id);
            if (dbSlider == null)
            {
                var newSlider = mapper.Map<Entity.Entities.Site.Slider.Slider>(data);
                await _dbContext.Sliders.AddAsync(newSlider);
            }
            else mapper.Map(data, dbSlider);
            if (data.IsHome)
            {
                var dbCheckCurrentHome = _dbContext.Sliders.Where(x => x.IsHome && x.LanguageId == data.LanguageId).ToList();
                if (dbCheckCurrentHome.Count > 0)
                {
                    foreach (var item in dbCheckCurrentHome)
                    {
                        if (item.Id != data.Id) item.IsHome = false;
                    }
                }
            }
            await _dbContext.SaveChangesAsync();
        }
        public async Task DeleteSlider(Guid sliderId)
        {
            if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
            if (sliderId == Guid.Empty) throw new ApiException(HttpStatusCode.NotAcceptable, "Please provide data correctly.");
            var dbSlider = await _dbContext.Sliders.FindAsync(sliderId);
            if (dbSlider == null) throw new ApiException(HttpStatusCode.NotFound, "Menu couldn't be found");
            _dbContext.Sliders.Remove(dbSlider);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveSliderItem(rm_saveslideritem data)
        {
            if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
            if (data.Name == null) throw new ApiException(HttpStatusCode.NotAcceptable, "Please define a slider name");
            var dbSliderItem = await _dbContext.Slider_Items.FindAsync(data.Id);
            if (dbSliderItem == null)
            {
                var newSliderItem = mapper.Map<Entity.Entities.Site.Slider.Slider_Item>(data);
                await _dbContext.Slider_Items.AddAsync(newSliderItem);
            }
            else mapper.Map(data, dbSliderItem);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteSliderItem(Guid itemId)
        {
            if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
            if (itemId == Guid.Empty) throw new ApiException(HttpStatusCode.NotAcceptable, "Please provide data correctly.");
            var dbSliderItem = await _dbContext.Slider_Items.FindAsync(itemId);
            if (dbSliderItem == null) throw new ApiException(HttpStatusCode.NotFound, "Menu couldn't be found");
            _dbContext.Slider_Items.Remove(dbSliderItem);
            await _dbContext.SaveChangesAsync();
        }
    }
}
