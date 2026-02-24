using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Services.ExchangeOffice.AutoRate
{
    /// <summary>
    /// Dış kaynaklardan kur çekme interface'i
    /// </summary>
    public interface IExternalRateProvider
    {
        /// <summary>
        /// Kaynak adı (TCMB, Harem, Binance vb.)
        /// </summary>
        string SourceName { get; }

        /// <summary>
        /// Kaynak key'i (TCMB, DOVIZ_COM_HAREM vb.)
        /// </summary>
        string SourceKey { get; }

        /// <summary>
        /// Kurları çek
        /// </summary>
        /// <returns>Döviz kodu ve rate bilgileri</returns>
        Task<List<ExternalRateDto>> FetchRatesAsync();

        /// <summary>
        /// Sağlık kontrolü
        /// </summary>
        Task<bool> IsHealthyAsync();
    }

    /// <summary>
    /// Dış kaynaktan gelen kur bilgisi
    /// </summary>
    public class ExternalRateDto
    {
        public string? Source { get; set; }
        public string? CurrencyCode { get; set; }
        public string? TargetCurrencyCode { get; set; } = "TRY";
        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }
        public decimal SpreadPercent => SellRate > 0 && BuyRate > 0
            ? ((SellRate - BuyRate) / BuyRate) * 100
            : 0;
        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
        public bool IsValid { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? SourceUrl { get; set; }
    }
}
