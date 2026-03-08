using AutoMapper;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Office
{
    public class OfficeServiceCommand : IOfficeServiceCommand
    {
        private readonly BaskentEnerjiDbContext _dbContext;
        private readonly IMapper mapper;
        private readonly ValidationService _validationService;

        public OfficeServiceCommand(BaskentEnerjiDbContext dbContext, IMapper mapper, ValidationService validationService)
        {
            _dbContext = dbContext;
            this.mapper = mapper;
            _validationService = validationService;
        }
        public async Task RemoveOffice(Guid id)
        {
            if (!await _validationService.IsOwnerAsync()) throw new ApiException(HttpStatusCode.Unauthorized, "Şube silme işlemi için Owner yetkisi gereklidir.");

            var dbOffice = _dbContext.Offices.FirstOrDefault(x => x.Id == id);
            if (dbOffice != null) _dbContext.Offices.Remove(dbOffice);

            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveOffice(rm_saveoffice data)
        {

            if (!await _validationService.IsOwnerAsync()) throw new ApiException(HttpStatusCode.Unauthorized, "Şube ekleme/düzenleme işlemi için Owner yetkisi gereklidir.");

            if (data.OfficeName == null) throw new ApiException(System.Net.HttpStatusCode.NoContent, "No content given");

            var dbOffice = _dbContext.Offices.FirstOrDefault(x => x.Id == data.Id);

            if (dbOffice != null)
            {
                // update
                _dbContext.Entry(dbOffice).CurrentValues.SetValues(mapper.Map<Entity.Entities.ExchangeOffice.Office.Office>(data));
            }
            else
            {
                data.Id = Guid.NewGuid();
                await _dbContext.Offices.AddAsync(mapper.Map<Entity.Entities.ExchangeOffice.Office.Office>(data));
            }

            await _dbContext.SaveChangesAsync();

        }
    }
}
