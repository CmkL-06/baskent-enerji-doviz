using SmileMedical.Entity.Entities.ExchangeOffice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Modals.RequestModals.ExchangeService.Office
{
    public class rm_savevault
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public Guid OfficeId { get; set; }
       // public VaultType? Type { get; set; } = VaultType.Main;
        public bool IsActive { get; set; } = true;


        public enum VaultType
        {
            Main = 1,
            Secondary = 2,
            Reserve = 3
        }
    }
}
