using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office
{
    public class rm_updatevaultbalance
    {
        public Guid vaultId { get; set; }
        public Guid currencyId { get; set; }
        public decimal amount { get; set; }
        public string? description { get; set; }
        public bool  isEntireBalance { get; set; }
      
        public TransactionType? TransactionType { get; set; }

        // Bu bakiye güncellemesi bir ExpensePayment onayından tetiklendiyse, oluşturulacak
        // VaultBalanceHistory kaydına doğrudan FK olarak yazılır. Boşsa serbest bir kasa hareketidir.
        public Guid? ExpensePaymentId { get; set; }
    }
}
