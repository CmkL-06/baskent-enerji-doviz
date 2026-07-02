using System;
using System.Collections.Generic;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office
{
    public class rm_dayclosure
    {
        public Guid OfficeId { get; set; }
        public DateTime BusinessDate { get; set; }
        public string Notes { get; set; }
        public List<rm_dayclosuredetail> Details { get; set; }
    }

    public class rm_dayclosuredetail
    {
        public Guid CurrencyId { get; set; }
        public decimal PhysicalCount { get; set; }
        public string DiscrepancyNote { get; set; }
    }
}
