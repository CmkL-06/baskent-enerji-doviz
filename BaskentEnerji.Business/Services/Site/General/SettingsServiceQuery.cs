using AutoMapper;
using BaskentEnerji.Business.Infrastructure.Site.General;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Modals.ResponseModals.Site.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.Site.General
{
    public class SettingsServiceQuery : ISettingsServiceQuery
    {
        private readonly BaskentEnerjiDbContext _dbContext;
        private readonly IMapper mapper;

        public SettingsServiceQuery(BaskentEnerjiDbContext dbContext, IMapper mapper)
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
