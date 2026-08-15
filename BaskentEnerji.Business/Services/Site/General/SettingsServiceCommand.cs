using AutoMapper;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.Site.General;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.Site;
using BaskentEnerji.Entity.Modals.RequestModals.Site.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.Site.General
{
    public class SettingsServiceCommand : ISettingsServiceCommand
    {
        private readonly BaskentEnerjiDbContext _dbContext;
        private readonly IMapper mapper;
        private readonly ValidationService _validationService;

        public SettingsServiceCommand(BaskentEnerjiDbContext dbContext, IMapper mapper, ValidationService validationService)
        {
            _dbContext = dbContext;
            this.mapper = mapper;
            _validationService = validationService;
        }
        public async Task SaveSettings(rm_settings_save data)
        {
           if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

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
