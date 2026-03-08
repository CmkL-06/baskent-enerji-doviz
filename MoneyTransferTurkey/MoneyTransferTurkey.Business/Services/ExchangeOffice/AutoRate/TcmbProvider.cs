using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MoneyTransferTurkey.Business.Services.ExchangeOffice.AutoRate
{
    /// <summary>
    /// TCMB (Türkiye Cumhuriyet Merkez Bankası) kur sağlayıcı
    /// </summary>
    public class TcmbProvider : IExternalRateProvider
    {
        private readonly HttpClient _httpClient;
        private const string TCMB_URL = "https://www.tcmb.gov.tr/kurlar/today.xml";

        public string SourceName => "TCMB";
        public string SourceKey => "TCMB";

        public TcmbProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ExternalRateDto>> FetchRatesAsync()
        {
            var rates = new List<ExternalRateDto>();

            try
            {
                var response = await _httpClient.GetStringAsync(TCMB_URL);
                var doc = XDocument.Parse(response);

                var currencies = doc.Descendants("Currency");

                foreach (var currency in currencies)
                {
                    try
                    {
                        var currencyCode = currency.Attribute("CurrencyCode")?.Value;
                        if (string.IsNullOrEmpty(currencyCode))
                            continue;

                        // ForexBuying ve ForexSelling kullanıyoruz (döviz alış/satış)
                        var forexBuyingStr = currency.Element("ForexBuying")?.Value;
                        var forexSellingStr = currency.Element("ForexSelling")?.Value;

                        if (string.IsNullOrEmpty(forexBuyingStr) || string.IsNullOrEmpty(forexSellingStr))
                            continue;

                        // TCMB virgül kullanıyor, nokta'ya çeviriyoruz
                        forexBuyingStr = forexBuyingStr.Replace(',', '.');
                        forexSellingStr = forexSellingStr.Replace(',', '.');

                        if (!decimal.TryParse(forexBuyingStr, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var buyRate))
                            continue;

                        if (!decimal.TryParse(forexSellingStr, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var sellRate))
                            continue;

                        // Unit kontrolü (bazı kurlar 100 birim üzerinden verilir)
                        var unitStr = currency.Element("Unit")?.Value;
                        if (!string.IsNullOrEmpty(unitStr) && decimal.TryParse(unitStr, out var unit) && unit > 1)
                        {
                            buyRate /= unit;
                            sellRate /= unit;
                        }

                        rates.Add(new ExternalRateDto
                        {
                            Source = SourceKey,
                            CurrencyCode = currencyCode,
                            TargetCurrencyCode = "TRY",
                            BuyRate = buyRate,
                            SellRate = sellRate,
                            IsValid = true,
                            SourceUrl = TCMB_URL,
                            FetchedAt = DateTime.UtcNow
                        });
                    }
                    catch (Exception ex)
                    {
                        // Tek bir currency hata verirse devam et
                        Console.WriteLine($"TCMB parse error for currency: {ex.Message}");
                    }
                }

                return rates;
            }
            catch (Exception ex)
            {
                // Genel hata - boş liste dön
                return new List<ExternalRateDto>
                {
                    new ExternalRateDto
                    {
                        Source = SourceKey,
                        IsValid = false,
                        ErrorMessage = $"TCMB fetch failed: {ex.Message}",
                        FetchedAt = DateTime.UtcNow
                    }
                };
            }
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(TCMB_URL);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
