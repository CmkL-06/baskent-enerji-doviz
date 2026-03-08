using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office
{
    public class rm_removetransaction
    {
        public Guid transactionId {  get; set; }
        public string reason {  get; set; }
    }
}
