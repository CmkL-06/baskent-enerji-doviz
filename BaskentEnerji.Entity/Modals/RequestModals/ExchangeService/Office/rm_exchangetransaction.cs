using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office
{
    public class rm_exchangetransaction
    {
        [Required]
        public Guid VaultId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? PartyId { get; set; }
        [Required]
        public Guid SourceCurrencyId { get; set; }
        [Required]
        public Guid TargetCurrencyId { get; set; }
        [Required]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Tutar sıfırdan büyük olmalıdır")]
        public decimal SourceAmount { get; set; }
        public bool IsBuyingFromCustomer { get; set; } // true = customer selling to us
        [Range(0.0001, (double)decimal.MaxValue, ErrorMessage = "Kur sıfırdan büyük olmalıdır")]
        public decimal? CustomRate { get; set; }
       // public decimal CommissionPercent { get; set; } = 0;
        public string? Notes { get; set; }
        public bool OwnerOverrideLoss { get; set; } = false;
    }
}
