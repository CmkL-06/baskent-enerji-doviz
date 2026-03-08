using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office
{
    public interface IUserOfficeService
    {
        Task<List<vm_useroffice>> GetUserOfficesAsync(Guid userId);
        Task<vm_useroffice> GetUserOfficeAsync(Guid userId, Guid officeId);
        Task<List<vm_office>> GetOfficesByUserAsync(Guid userId);
        Task<vm_useroffice> AttachOfficeToUserAsync(rm_useroffice model);
        Task<bool> RemoveOfficeFromUserAsync(Guid userId, Guid officeId);
        Task<bool> UpdateUserOfficesAsync(Guid userId, List<Guid> officeIds);
        Task<bool> HasUserAccessToOfficeAsync(Guid userId, Guid officeId);
        Task<List<BaskentEnerji.Entity.Modals.ViewModals.User.vm_user>> GetUsersByOfficeAsync(Guid officeId);
    }
}