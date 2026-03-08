using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Blockchain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Blockchain
{
    public class TRC20Service : ITRC20Service
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TRC20Service> _logger;
        private readonly string _tronscanApiUrl = "https://apilist.tronscan.org/api";
        private readonly string _usdtContract = "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t"; // USDT TRC20 Contract

        public TRC20Service(
            IHttpClientFactory httpClientFactory,
            ILogger<TRC20Service> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<object> GetRecentDepositsAsync(string walletAddress)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                
                // Get recent USDT TRC20 transfers to this address (deposits only)
                var url = $"{_tronscanApiUrl}/token_trc20/transfers?" +
                         $"toAddress={walletAddress}" +
                         $"&contract_address={_usdtContract}" +
                         $"&limit=20" +
                         $"&sort=-timestamp";

                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<TronscanResponse>(content);

                    if (data?.data != null)
                    {
                        var deposits = data.data.Select(t => new
                        {
                            txId = t.transaction_id,
                            from = t.from_address,
                            to = t.to_address,
                            amount = decimal.Parse(t.quant ?? "0") / 1000000m, // Convert from 6 decimals
                            timestamp = DateTimeOffset.FromUnixTimeMilliseconds(t.block_timestamp).DateTime,
                            confirmed = t.confirmed
                        }).ToList();

                        return new
                        {
                            wallet = walletAddress,
                            token = "USDT",
                            network = "TRC20",
                            deposits = deposits,
                            count = deposits.Count,
                            lastUpdated = DateTime.UtcNow
                        };
                    }
                }

                return new
                {
                    wallet = walletAddress,
                    token = "USDT",
                    network = "TRC20",
                    deposits = new List<object>(),
                    count = 0,
                    lastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching USDT deposits for {WalletAddress}", walletAddress);
                throw new Exception($"Failed to fetch deposits: {ex.Message}");
            }
        }

        private class TronscanResponse
        {
            public List<TronscanTransfer> data { get; set; }
            public int total { get; set; }
        }

        private class TronscanTransfer
        {
            public string transaction_id { get; set; }
            public string from_address { get; set; }
            public string to_address { get; set; }
            public long block_timestamp { get; set; }
            public string quant { get; set; }
            public bool confirmed { get; set; }
        }
    }
}