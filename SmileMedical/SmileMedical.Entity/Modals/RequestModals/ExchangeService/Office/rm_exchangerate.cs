using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Modals.RequestModals.ExchangeService.Office
{
    public class CreateExchangeRateRequest
    {
        [Required]
        public Guid OfficeId { get; set; }
        
        [Required]
        public Guid SourceCurrencyId { get; set; }
        
        [Required]
        public Guid TargetCurrencyId { get; set; }
        
        [Required]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Buy rate must be greater than 0")]
        public decimal BuyRate { get; set; }
        
        [Required]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Sell rate must be greater than 0")]
        public decimal SellRate { get; set; }
    }
}