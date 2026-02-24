using System;

namespace AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Currency
{
    /// <summary>
    /// Dış kaynaklardan çekilen kur verileri (cache + log)
    /// </summary>
    public class ExternalRateCache : BaseEntity
    {
        /// <summary>
        /// Kaynak adı: TCMB, DOVIZ_COM_HAREM, DOVIZ_COM_ZIRAAT,
        /// DOVIZ_COM_PTT, DOVIZ_COM_KAPALICARSI, BINANCE
        /// </summary>
        public string? Source { get; set; }

        public string? CurrencyCode { get; set; } // USD, EUR, RUB...
        public string? TargetCurrencyCode { get; set; } = "TRY";

        // Kurlar
        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }
        public decimal SpreadPercent { get; set; }

        // Zaman damgası
        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;

        // Geçerlilik
        public bool IsValid { get; set; } = true;
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Kaynak tam URL'i (debugging için)
        /// </summary>
        public string? SourceUrl { get; set; }
    }
}
