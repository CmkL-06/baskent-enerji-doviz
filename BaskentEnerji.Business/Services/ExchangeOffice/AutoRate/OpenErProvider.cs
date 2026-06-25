using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.AutoRate
{
    /// <summary>
    /// open.er-api.com provider — TCMB'de bulunmayan kurlar icin (CZK, BGN, KGS vb.)
    /// Ucretsiz, kayit gerektirmez, gunluk 1500 istek limiti
    /// </summary>
    public class OpenErProvider : IExternalRateProvider
    {
        private readonly HttpClient _httpClient;
        private const string API_URL = "https://open.er-api.com/v6/latest/EUR";
        private const decimal SPREAD_PCT = 1.5m; // %1.5 spread

        // TCMB'de olmayan ve bu kaynaktan cekilecek kurlar
        private static readonly HashSet<string> TargetCurrencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "CZK", "BGN", "KGS"
        };

        public string SourceName => "OpenER";
        public string SourceKey => "OPEN_ER";

        public OpenErProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ExternalRateDto>> FetchRatesAsync()
        {
            var result = new List<ExternalRateDto>();

            try
            {
                var response = await _httpClient.GetStringAsync(API_URL);
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                if (!root.TryGetProperty("result", out var status) || status.GetString() != "success")
                    return result;

                if (!root.TryGetProperty("rates", out var rates))
                    return result;

                // EUR/TRY
                if (!rates.TryGetProperty("TRY", out var tryEl))
                    return result;

                var eurTry = tryEl.GetDecimal();
                if (eurTry == 0) return result;

                foreach (var code in TargetCurrencies)
                {
                    if (!rates.TryGetProperty(code, out var rateEl)) continue;

                    var perEur = rateEl.GetDecimal();
                    if (perEur == 0) continue;

                    var mid = eurTry / perEur;
                    var buy  = Math.Round(mid * (1 - SPREAD_PCT / 100), 6);
                    var sell = Math.Round(mid * (1 + SPREAD_PCT / 100), 6);

                    result.Add(new ExternalRateDto
                    {
                        Source = SourceName,
                        CurrencyCode = code.ToUpperInvariant(),
                        TargetCurrencyCode = "TRY",
                        BuyRate = buy,
                        SellRate = sell,
                        FetchedAt = DateTime.UtcNow,
                        IsValid = true,
                        SourceUrl = API_URL
                    });
                }
            }
            catch
            {
                // provider hatasi auto-rate'i durdurmasin
            }

            return result;
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var resp = await _httpClient.GetAsync(API_URL);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}
