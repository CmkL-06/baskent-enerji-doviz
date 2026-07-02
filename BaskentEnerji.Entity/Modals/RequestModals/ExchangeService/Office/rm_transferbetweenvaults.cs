using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office
{
    public class rm_transferbetweenvaults
    {
        [Required]
        public Guid SourceVaultId { get; set; }
        [Required]
        public Guid TargetVaultId { get; set; }
        [Required]
        public Guid CurrencyId { get; set; }
        [Required]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Tutar sıfırdan büyük olmalıdır")]
        public decimal Amount { get; set; }
        public string Notes { get; set; }
    }
}
