using BaskentEnerji.Entity.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_office : BaseEntity
    {
        public string OfficeName { get; set; }
        public string? OfficeDescription { get; set; }
        public string? OfficeImageUri { get; set; }
       
    }
}
