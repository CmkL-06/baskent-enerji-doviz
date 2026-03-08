using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.AutoRate
{
    /// <summary>
    /// Binance Public API provider (crypto kurları)
    /// </summary>
    public class BinanceProvider : IExternalRateProvider
    {
        private readonly HttpClient _httpClient;
        private const string BINANCE_API = "https://api.binance.com/api/v3/ticker/price";

        // Crypto pairs to fetch
        private static readonly List<string> Symbols = new()
        {
            "USDTTRY",  // USDT/TRY
            "BTCTRY",   // BTC/TRY
            "ETHTRY",   // ETH/TRY
            "USDTUSD"   // USDT/USD (important: USDT ≠ USD)
        };

        public string SourceName => "Binance";
        public string SourceKey => "BINANCE";

        public BinanceProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ExternalRateDto>> FetchRatesAsync()
        {
            var rates = new List<ExternalRateDto>();

            foreach (var symbol in Symbols)
            {
                try
                {
                    var rate = await FetchSinglePairAsync(symbol);
                    if (rate != null)
                    {
                        rates.Add(rate);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Binance fetch error for {symbol}: {ex.Message}");
                }
            }

            if (rates.Count == 0)
            {
                rates.Add(new ExternalRateDto
                {
                    Source = SourceKey,
                    IsValid = false,
                    ErrorMessage = "No rates fetched from Binance",
                    FetchedAt = DateTime.UtcNow
                });
            }

            return rates;
        }

        private async Task<ExternalRateDto> FetchSinglePairAsync(string symbol)
        {
            var url = $"{BINANCE_API}?symbol={symbol}";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var data = JsonSerializer.Deserialize<BinanceTickerResponse>(response);

                if (data == null || string.IsNullOrEmpty(data.symbol) || string.IsNullOrEmpty(data.price))
                    return null;

                if (!decimal.TryParse(data.price, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var price))
                    return null;

                // Parse currency codes from symbol
                // USDTTRY -> USDT/TRY
                // BTCTRY -> BTC/TRY
                // USDTUSD -> USDT/USD
                var (baseCurrency, quoteCurrency) = ParseSymbol(symbol);

                // Binance sadece spot fiyat veriyor, bid/ask yok
                // Küçük bir spread ekleyelim (%0.1)
                var spreadPercent = 0.001m; // %0.1
                var buyRate = price * (1 - spreadPercent);
                var sellRate = price * (1 + spreadPercent);

                return new ExternalRateDto
                {
                    Source = SourceKey,
                    CurrencyCode = baseCurrency,
                    TargetCurrencyCode = quoteCurrency,
                    BuyRate = buyRate,
                    SellRate = sellRate,
                    IsValid = true,
                    SourceUrl = url,
                    FetchedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new ExternalRateDto
                {
                    Source = SourceKey,
                    IsValid = false,
                    ErrorMessage = $"Binance {symbol} error: {ex.Message}",
                    SourceUrl = url,
                    FetchedAt = DateTime.UtcNow
                };
            }
        }

        private (string baseCurrency, string quoteCurrency) ParseSymbol(string symbol)
        {
            // Known symbols
            if (symbol == "USDTTRY") return ("USDT", "TRY");
            if (symbol == "BTCTRY") return ("BTC", "TRY");
            if (symbol == "ETHTRY") return ("ETH", "TRY");
            if (symbol == "USDTUSD") return ("USDT", "USD");
            if (symbol == "USDTUSDC") return ("USDT", "USDC");

            // Generic parsing (last 3 chars = quote currency)
            if (symbol.Length > 3)
            {
                var quote = symbol.Substring(symbol.Length - 3);
                var base_ = symbol.Substring(0, symbol.Length - 3);
                return (base_, quote);
            }

            return (symbol, "TRY");
        }

        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BINANCE_API}?symbol=USDTTRY");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private class BinanceTickerResponse
        {
            public string symbol { get; set; }
            public string price { get; set; }
        }
    }
}
