using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.ExchangeService
{
    public class rm_convertcurrency
    {
       public Guid sourceCurrencyId { get; set; }
       public Guid targetCurrencyId { get; set; }
       public decimal amount { get; set; }
       public bool isBuyingFromCustomer { get; set; }
    }
}
