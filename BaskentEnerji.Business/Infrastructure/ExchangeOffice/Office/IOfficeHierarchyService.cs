using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office
{
    public interface IOfficeHierarchyService
    {
        /// <summary>Tüm ofisleri Merkez → Şube/Bayi ağaç yapısında döner.</summary>
        Task<List<vm_office>> GetHierarchyAsync();

        /// <summary>Merkez ofisi döner (OfficeType == Merkez).</summary>
        Task<vm_office?> GetMerkezAsync();

        /// <summary>Belirli bir üst ofise bağlı alt birimleri döner.</summary>
        Task<List<vm_office>> GetChildrenAsync(Guid parentOfficeId);

        /// <summary>Kullanıcının erişebildiği ofisleri ofis rolüyle birlikte döner.</summary>
        Task<List<vm_useroffice>> GetUserAccessibleOfficesAsync(Guid userId);
    }
}
