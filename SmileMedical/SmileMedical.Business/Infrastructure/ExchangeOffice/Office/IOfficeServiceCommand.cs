using SmileMedical.Entity.Modals.RequestModals.ExchangeService.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Infrastructure.ExchangeOffice.Office
{
    public interface IOfficeServiceCommand
    {
        Task SaveOffice(rm_saveoffice data);
        Task RemoveOffice(Guid id);
    }
}
