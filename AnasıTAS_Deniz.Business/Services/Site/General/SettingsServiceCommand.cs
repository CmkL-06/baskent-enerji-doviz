using AutoMapper;
using AnasıTAS_Deniz.Business.Infrastructure.Site.General;
using AnasıTAS_Deniz.Data.Contexts;
using AnasıTAS_Deniz.Entity.Entities.Site;
using AnasıTAS_Deniz.Entity.Modals.RequestModals.Site.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Services.Site.General
{
    public class SettingsServiceCommand : ISettingsServiceCommand
    {
        private readonly AnasıTAS_DenizDbContext _dbContext;
        private readonly IMapper mapper;

        public SettingsServiceCommand(AnasıTAS_DenizDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            this.mapper = mapper;
        }
        public async Task SaveSettings(rm_settings_save data)
        {
           Site_Settings nSetting = mapper.Map<Site_Settings>(data);
           
            // _dbContext.Site_Settings.Update(nSetting);
            var dbSetting = _dbContext.Site_Settings.FirstOrDefault();
            if (dbSetting != null)
            {
                nSetting.Id = dbSetting.Id;
                _dbContext.Entry(dbSetting).CurrentValues.SetValues(nSetting);
            } else await _dbContext.Site_Settings.AddAsync(nSetting);
            
            await _dbContext.SaveChangesAsync();
        }
    }
}
