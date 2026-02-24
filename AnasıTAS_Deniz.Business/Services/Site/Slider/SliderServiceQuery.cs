using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using AnasıTAS_Deniz.Business.Exceptions;
using AnasıTAS_Deniz.Business.Infrastructure.Site.Slider;
using AnasıTAS_Deniz.Business.Services.Permission;
using AnasıTAS_Deniz.Data.Contexts;
using AnasıTAS_Deniz.Entity.Modals.RequestModals.Site.Slider;
using AnasıTAS_Deniz.Entity.Modals.ViewModals.Slider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Services.Site.Slider
{
    public class SliderServiceQuery : ISliderServiceQuery
    {
        private readonly AnasıTAS_DenizDbContext _dbContext;
        private readonly ValidationService _validationService;
        private readonly IMapper mapper;

        public SliderServiceQuery(AnasıTAS_DenizDbContext dbContext, ValidationService validationService, IMapper mapper)
        {
            _dbContext = dbContext;
            _validationService = validationService;
            this.mapper = mapper;
        }
        public async Task<List<vm_slider>> GetSliders(rm_slider data)
        {
            if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
            var query = _dbContext.Sliders.AsQueryable();
            query = query.Include(x => x.Slider_Items);

            if (data.IsBlog.HasValue) query = query.Where(x => x.IsBlog == data.IsBlog.Value);
            if (data.IsEnabled.HasValue) query = query.Where(x => x.IsEnabled == data.IsEnabled.Value);
            if (data.IsHome.HasValue) query = query.Where(x => x.IsHome == data.IsHome.Value);
            if (data.Id.HasValue) query = query.Where(x => x.Id == data.Id.Value);
            if (data.LanguageId.HasValue) query = query.Where(x => x.LanguageId == data.LanguageId.Value);
            if (!string.IsNullOrEmpty(data.LanguageCode)) query = query.Where(x => x.Language.LanguageCode == data.LanguageCode);
            return await query.ProjectTo<vm_slider>(mapper.ConfigurationProvider).OrderByDescending(x => x.CreatedDate).ToListAsync();
        }

        public async Task<List<vm_slider_guest>> GetSliders_Guest(rm_slider data)
        {
            var query = _dbContext.Sliders.Include(x=> x.Slider_Items).AsQueryable();
          

          

            if (data.IsBlog.HasValue) query = query.Where(x => x.IsBlog == data.IsBlog.Value);
            if (data.IsEnabled.HasValue) query = query.Where(x => x.IsEnabled == data.IsEnabled.Value);
            if (data.IsHome.HasValue) query = query.Where(x => x.IsHome == data.IsHome.Value);
            if (data.Id.HasValue) query = query.Where(x => x.Id == data.Id.Value);
            if (data.LanguageId.HasValue) query = query.Where(x => x.LanguageId == data.LanguageId.Value);
            if (!string.IsNullOrEmpty(data.LanguageCode)) query = query.Where(x => x.Language.LanguageCode == data.LanguageCode);

            var sliders = await query.ToListAsync();

            foreach (var slider in sliders)
            {
                slider.Slider_Items = slider.Slider_Items.OrderBy(si => si.Order).ToList();
            }

            return sliders.Select(s => new vm_slider_guest
            {
                Name = s.Name,
                CreatedDate = s.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff"),
                SliderItems = s.Slider_Items.Select(si => new vm_slideritem_guest
                {
                    Name = si.Name,
                    Title = si.Title,
                    Order = si.Order,
                    Description = si.Description,
                    Description2 = si.Description2,
                    CallText = si.CallText,
                    ImageUrl = si.ImageUrl,
                    VideoUrl = si.VideoUrl
                }).ToList()
            }).OrderByDescending(x => x.CreatedDate).ToList();

            //  return await query.ProjectTo<vm_slider_guest>(mapper.ConfigurationProvider).OrderByDescending(x => x.CreatedDate).ToListAsync();
        }
    }
}
