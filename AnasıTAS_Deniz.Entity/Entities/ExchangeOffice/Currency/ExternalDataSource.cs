using System;

namespace AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Currency
{
    /// <summary>
    /// Dış veri kaynakları tanımları (ayarlanabilir)
    /// </summary>
    public class ExternalDataSource : BaseEntity
    {
        /// <summary>
        /// Görünen ad: TCMB, Harem, Ziraat, PTT...
        /// </summary>
        public string? SourceName { get; set; }

        /// <summary>
        /// Kaynak tipi: API, WEB_SCRAPE
        /// </summary>
        public string? SourceType { get; set; }

        /// <summary>
        /// Sistem içinde kullanılan key: TCMB, DOVIZ_COM_HAREM...
        /// </summary>
        public string? SourceKey { get; set; }

        /// <summary>
        /// Base URL
        /// </summary>
        public string? BaseUrl { get; set; }

        /// <summary>
        /// Aktif/Pasif
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Öncelik (1=en yüksek, 5=en düşük)
        /// </summary>
        public int Priority { get; set; } = 5;

        /// <summary>
        /// Ek konfigürasyon (JSON)
        /// Örn: {"timeout": 5000, "retryCount": 3}
        /// </summary>
        public string? Configuration { get; set; }

        /// <summary>
        /// Son başarılı veri çekme
        /// </summary>
        public DateTime? LastSuccessfulFetch { get; set; }

        /// <summary>
        /// Son hata mesajı
        /// </summary>
        public string? LastError { get; set; }
    }
}
