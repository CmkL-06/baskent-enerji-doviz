using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Services.ExchangeOffice.AutoRate
{
    /// <summary>
    /// Doviz.com web scraping provider
    /// Supports: Harem, Ziraat, PTT, Kapalıçarşı
    /// </summary>
    public class DovizComProvider : IExternalRateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _exchangeKey; // "harem", "ziraat", "ptt", "kapali-carsi"

        private const string BASE_URL = "https://kur.doviz.com";

        // Currency mapping (doviz.com URL format)
        private static readonly Dictionary<string, string> CurrencyUrls = new()
        {
            { "USD", "amerikan-dolari" },
            { "EUR", "euro" },
            { "GBP", "sterlin" },
            { "CHF", "isvicre-frangi" },
            { "CAD", "kanada-dolari" },
            { "RUB", "rus-rublesi" },
            { "AUD", "avustralya-dolari" },
            { "DKK", "danimarka-kronu" },
            { "SEK", "isvec-kronu" },
            { "NOK", "norveç-kronu" },
            { "JPY", "japon-yeni" },
            { "KWD", "kuveyt-dinari" },
            { "SAR", "suudi-arabistan-riyali" },
            { "AED", "birleşik-arap-emirlikleri-dirhemi" },
            // Yeni eklenen dövizler
            { "GEL", "gürcistan-larisi" },
            { "HKD", "hong-kong-dolari" },
            { "KZT", "kazakistan-tengesi" },
            { "UAH", "ukrayna-grivnasi" },
            { "CNY", "cin-yuani" },
            { "INR", "hindistan-rupisi" },
            { "THB", "tayland-bahti" },
            { "MYR", "malezya-ringgiti" },
            { "SGD", "singapur-dolari" },
            { "NZD", "yeni-zelanda-dolari" }
        };

        public string SourceName { get; }
        public string SourceKey { get; }

        public DovizComProvider(HttpClient httpClient, string exchangeKey, string sourceName)
        {
            _httpClient = httpClient;
            _exchangeKey = exchangeKey;
            SourceName = sourceName;
            SourceKey = $"DOVIZ_COM_{exchangeKey.ToUpper().Replace("-", "")}";
        }

        public async Task<List<ExternalRateDto>> FetchRatesAsync()
        {
            var rates = new List<ExternalRateDto>();

            foreach (var (currencyCode, urlSlug) in CurrencyUrls)
            {
                try
                {
                    var rate = await FetchSingleCurrencyAsync(currencyCode, urlSlug);
                    if (rate != null)
                    {
                        rates.Add(rate);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DovizCom fetch error for {currencyCode}: {ex.Message}");
                }
            }

            if (rates.Count == 0)
            {
                rates.Add(new ExternalRateDto
                {
                    Source = SourceKey,
                    IsValid = false,
                    ErrorMessage = $"No rates fetched from {SourceName}",
                    FetchedAt = DateTime.UtcNow
                });
            }

            return rates;
        }

        private async Task<ExternalRateDto?> FetchSingleCurrencyAsync(string currencyCode, string urlSlug)
        {
            // PTT ve Ziraat için farklı URL yapısı
            var url = _exchangeKey switch
            {
                "ptt" => $"https://kur.doviz.com/serbest-piyasa/{urlSlug}",
                "ziraat" => $"https://kur.doviz.com/serbest-piyasa/{urlSlug}",
                _ => $"{BASE_URL}/{_exchangeKey}/{urlSlug}"
            };

            try
            {
                var html = await _httpClient.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Try different table selectors based on site structure
                var rows = doc.DocumentNode.SelectNodes("//table//tr") ??
                          doc.DocumentNode.SelectNodes("//div[@class='table-responsive']//table//tr") ??
                          doc.DocumentNode.SelectNodes("//div[contains(@class,'currency-table')]//tr");

                if (rows == null || rows.Count == 0)
                {
                    // Log for debug
                    Console.WriteLine($"No table rows found for {SourceName} at {url}");
                    return null;
                }

                // İlk satır header, sonraki satırlar data
                // PTT ve Ziraat için serbest piyasadan al, diğerleri için ilk satırı al
                int skipRows = (_exchangeKey == "ptt" || _exchangeKey == "ziraat") ? 0 : 1;

                foreach (var row in rows.Skip(skipRows).Take(5)) // İlk 5 satırı kontrol et
                {
                    var cells = row.SelectNodes(".//td");
                    if (cells == null || cells.Count < 3)
                        continue;

                    // Eğer PTT veya Ziraat ise, ilk geçerli değeri al
                    // Değilse banka adını kontrol et
                    if (_exchangeKey != "ptt" && _exchangeKey != "ziraat")
                    {
                        var bankName = cells[0].InnerText.Trim().ToLower();
                        // Harem ve Kapalıçarşı için özel kontrol
                        if (_exchangeKey == "harem" && !bankName.Contains("harem") && !bankName.Contains("serbest"))
                            continue;
                        if (_exchangeKey == "kapali-carsi" && !bankName.Contains("kapalı") && !bankName.Contains("kapali"))
                            continue;
                    }

                    var buyStr = cells[1].InnerText.Trim();
                    var sellStr = cells[2].InnerText.Trim();

                    // Parse rates (format: "41,9000" veya "41.9000")
                    buyStr = buyStr.Replace(".", "").Replace(",", ".");
                    sellStr = sellStr.Replace(".", "").Replace(",", ".");

                    if (!decimal.TryParse(buyStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var buyRate))
                        continue;

                    if (!decimal.TryParse(sellStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var sellRate))
                        continue;

                    return new ExternalRateDto
                    {
                        Source = SourceKey,
                        CurrencyCode = currencyCode,
                        TargetCurrencyCode = "TRY",
                        BuyRate = buyRate,
                        SellRate = sellRate,
                        IsValid = true,
                        SourceUrl = url,
                        FetchedAt = DateTime.UtcNow
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                return new ExternalRateDto
                {
                    Source = SourceKey,
                    CurrencyCode = currencyCode,
                    IsValid = false,
                    ErrorMessage = $"Parse error: {ex.Message}",
                    SourceUrl = url,
                    FetchedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var url = $"{BASE_URL}/{_exchangeKey}/amerikan-dolari";
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
