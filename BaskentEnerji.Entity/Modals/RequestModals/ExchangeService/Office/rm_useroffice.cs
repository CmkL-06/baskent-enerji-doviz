using System;
using System.Collections.Generic;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office
{
    public class rm_useroffice
    {
        public Guid UserId { get; set; }
        public Guid OfficeId { get; set; }
    }

    public class rm_updateuseroffices
    {
        public Guid UserId { get; set; }
        public List<Guid> OfficeIds { get; set; }
    }
}