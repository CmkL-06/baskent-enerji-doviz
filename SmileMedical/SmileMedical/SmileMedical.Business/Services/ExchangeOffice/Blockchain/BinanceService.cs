using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SmileMedical.Business.Infrastructure.ExchangeOffice.Blockchain;

namespace SmileMedical.Business.Services.ExchangeOffice.Blockchain
{
    public class BinanceService : ITRC20Service
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<BinanceService> _logger;
        private readonly string _apiKey = "jaAPLL9RHEBb7BWaT608MTGjbs5nUxAetTLMClZvCzP1j1ehXFr2yorQ3Gv0hzA3";
        private readonly string _apiSecret = "hYDAPupzqvIf8wnUiCkOG2e4DszGzlFxVFcvly50im0VonPFNRqvGBspKJOu7hS6";
        private readonly string _binanceApiUrl = "https://api.binance.com";

        public BinanceService(
            IHttpClientFactory httpClientFactory,
            ILogger<BinanceService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<object> GetRecentDepositsAsync(string walletAddress = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                
                // Get deposit history from Binance
                var endpoint = "/sapi/v1/capital/deposit/hisrec";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                // Parameters for the request
                var parameters = $"coin=USDT&network=TRX&timestamp={timestamp}";
                
                // Create signature
                var signature = CreateSignature(parameters);
                var url = $"{_binanceApiUrl}{endpoint}?{parameters}&signature={signature}";
                
                // Add API key to headers
                client.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);
                
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var deposits = JsonConvert.DeserializeObject<List<BinanceDeposit>>(content);
                    
                    // Filter for USDT TRC20 deposits
                    var usdtDeposits = deposits?
                        .Where(d => d.coin == "USDT" && d.network == "TRX")
                        .Select(d => new
                        {
                            txId = d.txId,
                            amount = d.amount,
                            address = d.address,
                            addressTag = d.addressTag,
                            insertTime = DateTimeOffset.FromUnixTimeMilliseconds(d.insertTime).DateTime,
                            confirmTimes = $"{d.confirmTimes}/{d.unlockConfirm}",
                            status = GetStatusText(d.status)
                        })
                        .OrderByDescending(d => d.insertTime)
                        .ToList();
                    
                    return new
                    {
                        token = "USDT",
                        network = "TRC20",
                        deposits = usdtDeposits,
                        count = usdtDeposits?.Count ?? 0,
                        lastUpdated = DateTime.Now
                    };
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Binance API error: {Error}", error);
                    throw new Exception($"Binance API error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Binance deposits");
                throw new Exception($"Failed to fetch deposits: {ex.Message}");
            }
        }

        private string CreateSignature(string queryString)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiSecret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private string GetStatusText(int status)
        {
            return status switch
            {
                0 => "Pending",
                1 => "Success",
                6 => "Credited but cannot withdraw",
                _ => "Unknown"
            };
        }

        private class BinanceDeposit
        {
            public decimal amount { get; set; }
            public string coin { get; set; }
            public string network { get; set; }
            public int status { get; set; }
            public string address { get; set; }
            public string addressTag { get; set; }
            public string txId { get; set; }
            public long insertTime { get; set; }
            public long transferType { get; set; }
            public string confirmTimes { get; set; }
            public int unlockConfirm { get; set; }
            public string walletType { get; set; }
        }
    }
}