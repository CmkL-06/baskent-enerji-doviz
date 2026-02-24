using AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Office;
using AnasıTAS_Deniz.Entity.Modals.RequestModals.ExchangeService.Office;
using AnasıTAS_Deniz.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Infrastructure.ExchangeOffice.Office
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
        Task<List<AnasıTAS_Deniz.Entity.Modals.ViewModals.User.vm_user>> GetUsersByOfficeAsync(Guid officeId);
    }
}