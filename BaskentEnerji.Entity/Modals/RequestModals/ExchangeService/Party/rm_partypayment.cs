using BaskentEnerji.Entity.Entities.ExchangeOffice.Party;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Party
{
    public class rm_partypayment
    {
        [Required]
        public Guid PartyId { get; set; }
        [Required]
        public Guid CurrencyId { get; set; }
        [Required]
        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Tutar sıfırdan büyük olmalıdır")]
        public decimal Amount { get; set; }
        [Range(0.0001, (double)decimal.MaxValue, ErrorMessage = "Kur sıfırdan büyük olmalıdır")]
        public decimal? CustomExchangeRate { get; set; } // Özel kur (opsiyonel)
        public string PaymentMethod { get; set; }
        public string PaymentReference { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Notes { get; set; }
        public EntryType Type { get; set; }
        public Guid OfficeId { get; set; }
    }
}