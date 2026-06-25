using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office
{
    public interface IOfficeTransferService
    {
        /// <summary>Şube/Bayi transfer talebi oluşturur.</summary>
        Task<vm_officetransfer> CreateTransferRequestAsync(rm_create_officetransfer model, Guid requestedByUserId);

        /// <summary>Merkez: transfer onaylar veya reddeder.</summary>
        Task<vm_officetransfer> ProcessTransferAsync(Guid transferId, rm_action_officetransfer action, Guid approvedByUserId);

        /// <summary>Bekleyen transfer taleplerini listeler (Merkez görür).</summary>
        Task<List<vm_officetransfer>> GetPendingTransfersAsync();

        /// <summary>Ofise ait tüm transferleri döner.</summary>
        Task<List<vm_officetransfer>> GetTransfersByOfficeAsync(Guid officeId);

        /// <summary>Tek transfer detayı.</summary>
        Task<vm_officetransfer?> GetTransferByIdAsync(Guid transferId);
    }
}
