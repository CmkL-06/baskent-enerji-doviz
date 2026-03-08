using AutoMapper;
using BaskentEnerji.Business.Infrastructure.Site.General;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.Site;
using BaskentEnerji.Entity.Modals.RequestModals.Site.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.Site.General
{
    public class SettingsServiceCommand : ISettingsServiceCommand
    {
        private readonly BaskentEnerjiDbContext _dbContext;
        private readonly IMapper mapper;

        public SettingsServiceCommand(BaskentEnerjiDbContext dbContext, IMapper mapper)
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
