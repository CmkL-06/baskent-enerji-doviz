using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Party;
using System;
using System.Collections.Generic;

namespace MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Party
{
    public class rm_partypayment
    {
        public Guid PartyId { get; set; }
        public Guid CurrencyId { get; set; }
        public decimal Amount { get; set; }
        public decimal? CustomExchangeRate { get; set; } // Özel kur (opsiyonel)
        public string PaymentMethod { get; set; }
        public string PaymentReference { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Notes { get; set; }
        public EntryType Type { get; set; }
        public Guid OfficeId { get; set; }
    }
}