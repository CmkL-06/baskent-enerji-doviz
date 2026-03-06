using AutoMapper;
using SmileMedical.Business.Infrastructure.Site.General;
using SmileMedical.Data.Contexts;
using SmileMedical.Entity.Modals.ResponseModals.Site.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Services.Site.General
{
    public class SettingsServiceQuery : ISettingsServiceQuery
    {
        private readonly SmileMedicalDbContext _dbContext;
        private readonly IMapper mapper;

        public SettingsServiceQuery(SmileMedicalDbContext dbContext, IMapper mapper)
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
