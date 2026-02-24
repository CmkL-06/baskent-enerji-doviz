using AutoMapper;
using AnasıTAS_Deniz.Business.Infrastructure.Site.General;
using AnasıTAS_Deniz.Data.Contexts;
using AnasıTAS_Deniz.Entity.Modals.ResponseModals.Site.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Services.Site.General
{
    public class SettingsServiceQuery : ISettingsServiceQuery
    {
        private readonly AnasıTAS_DenizDbContext _dbContext;
        private readonly IMapper mapper;

        public SettingsServiceQuery(AnasıTAS_DenizDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            this.mapper = mapper;
        }
        public vm_settings GetSiteSettings()
        {
            return mapper.Map<vm_settings>(_dbContext.Site_Settings.FirstOrDefault());
        }
    }
}
