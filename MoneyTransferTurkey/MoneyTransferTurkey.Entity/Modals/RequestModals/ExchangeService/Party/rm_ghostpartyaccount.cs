using System;
using System.ComponentModel.DataAnnotations;

namespace MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Party
{
    public class rm_ghostpartyaccount
    {
        [Required]
        public Guid PartyId { get; set; }

        [Required]
        public Guid OfficeId { get; set; }

        [Required]
        public Guid CurrencyId { get; set; }

        public decimal InitialBalance { get; set; } = 0;

        [MaxLength(500)]
        public string Note { get; set; }
    }
}