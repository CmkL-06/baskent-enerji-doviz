using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Office
{
    public class VaultBalanceHistory : BaseEntity
    {
        public Guid VaultId { get; set; }
        public Guid CurrencyId { get; set; }
        public Guid? UserId { get; set; }
        public decimal Balance { get; set; }
       
       
        public string? Description { get; set; }

        public Vault Vault { get; set; }
        public Currency.Currency Currency { get; set; }
        public bool IsDeleted { get; set; }
        public TransactionType TransactionType { get; set; }
        public bool IsGhost { get; set; } = false;
        public bool IsParty { get; set; } = false;
    }
}
