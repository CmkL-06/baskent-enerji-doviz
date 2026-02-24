using Google.Apis.Logging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AnasıTAS_Deniz.Business.Hubs;
using AnasıTAS_Deniz.Business.Infrastructure.Coin;
using AnasıTAS_Deniz.Data.Contexts;
using AnasıTAS_Deniz.Entity.Entities.Site;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Services.Coin
{
    public class CoinPriceService 
    {
        private readonly IHubContext<CoinPriceHub> _hubContext;
        private readonly ILogger<CoinPriceService> _logger;
        private readonly ConcurrentDictionary<string, WebSocket> _coinSockets = new();
        
       
        public CoinPriceService(IHubContext<CoinPriceHub> hubContext, ILogger<CoinPriceService> logger)
        {
            _hubContext = hubContext;

            _logger = logger;
        }
        public async Task ReceivePriceUpdatesOld(WebSocket socket, string coinSymbol)
        {
            var buffer = new byte[1024 * 4]; // Buffer for WebSocket messages

            while (socket.State == WebSocketState.Open)
            {
                string jsonData = null;
                try
                {
                    // Receive data from the WebSocket
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }

                    // Convert the received bytes into a JSON string
                    jsonData = Encoding.UTF8.GetString(buffer, 0, result.Count);


                    // Parse the JSON data
                    using (JsonDocument document = JsonDocument.Parse(jsonData))
                    {
                        if (document.RootElement.TryGetProperty("p", out JsonElement priceElement))
                        {
                            //string priceString = priceElement.GetString();

                            // Intelligent decimal parsing
                            decimal price = decimal.Parse(priceElement.GetString(), System.Globalization.CultureInfo.InvariantCulture);


                          //  _logger.LogInformation("Parsed price for {CoinSymbol}: {Price}", coinSymbol, price);

                            await _hubContext.Clients.Group(coinSymbol).SendAsync("ReceiveCoinPrice", coinSymbol, price);
                        }
                        else
                        {
                            _logger.LogWarning("Price field missing in JSON for {CoinSymbol}: {JsonData}", coinSymbol, jsonData);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON parsing failed for {coinSymbol}: {ex.Message}");
                    Console.WriteLine($"Faulty JSON data: {jsonData}");
                }
                catch (FormatException ex)
                {
                    Console.WriteLine($"Failed to parse price as decimal for {coinSymbol}: {ex.Message}");
                    Console.WriteLine($"Faulty JSON data: {jsonData}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                    break; // Optionally break the loop if necessary
                }
               // await Task.Delay(1000); // 1 second delay
            }
        }
        public async Task ReceivePriceUpdates(WebSocket socket, string coinSymbol, string exchange)
        {
            var buffer = new byte[1024 * 4]; // Buffer for WebSocket messages
            while (socket.State == WebSocketState.Open)
            {
                string jsonData = null;
                try
                {
                    // Receive data from the WebSocket
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }

                    // Convert the received bytes into a JSON string
                    jsonData = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    // Parse the JSON data
                    using (JsonDocument document = JsonDocument.Parse(jsonData))
                    {
                        decimal price = 0;
                        bool priceFound = false;

                        // MEXC parsing
                        if (exchange == "mexc")
                        {
                            // Handling MEXC subscription confirmation
                            if (document.RootElement.TryGetProperty("code", out JsonElement codeElement) &&
                                codeElement.GetInt32() == 0)
                            {
                                _logger.LogInformation("MEXC Subscription confirmed for {CoinSymbol}", coinSymbol);
                                continue;
                            }

                            // Check if the message is for the specific coin we're tracking
                            bool isTargetCoin = false;
                            if (document.RootElement.TryGetProperty("s", out JsonElement symbolElement))
                            {
                                isTargetCoin = symbolElement.GetString().Equals(coinSymbol, StringComparison.OrdinalIgnoreCase);
                            }

                            // Only proceed if it's the target coin
                            if (isTargetCoin)
                            {
                                // 1. First format with direct 'p' property
                                if (document.RootElement.TryGetProperty("p", out JsonElement mexcDirectPriceElement))
                                {
                                    price = decimal.Parse(mexcDirectPriceElement.GetString(), System.Globalization.CultureInfo.InvariantCulture);
                                    priceFound = true;
                                }

                                // 2. First format with 'd' property
                                if (!priceFound &&
                                    document.RootElement.TryGetProperty("d", out JsonElement dElement) &&
                                    dElement.TryGetProperty("deals", out JsonElement dealsElement) &&
                                    dealsElement.GetArrayLength() > 0)
                                {
                                    var firstDeal = dealsElement[0];
                                    if (firstDeal.TryGetProperty("p", out JsonElement mexcDealsPriceElement))
                                    {
                                        price = decimal.Parse(mexcDealsPriceElement.GetString(), System.Globalization.CultureInfo.InvariantCulture);
                                        priceFound = true;
                                    }
                                }
                            }
                        }
                        // Binance parsing
                        else if (exchange == "binance")
                        {
                            if (document.RootElement.TryGetProperty("p", out JsonElement binancePriceElement))
                            {
                                price = decimal.Parse(binancePriceElement.GetString(), System.Globalization.CultureInfo.InvariantCulture);
                                priceFound = true;
                            }
                        }

                        // Send price if found
                        if (priceFound)
                        {
                            await _hubContext.Clients.Group(coinSymbol.ToLower()).SendAsync("ReceiveCoinPrice", coinSymbol, price);
                        }
                        else
                        {
                            _logger.LogWarning("Price not found in {Exchange} JSON for {CoinSymbol}: {JsonData}",
                                exchange, coinSymbol, jsonData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing {Exchange} WebSocket message for {CoinSymbol}: {Message}",
                        exchange, coinSymbol, ex.Message);
                    break;
                }
            }
        }
        public async Task StartTrackingCoinOld(string coinSymbol)
        {
            if (!_coinSockets.ContainsKey(coinSymbol))
            {
                var socket = new ClientWebSocket();

                // Add WebSocket options to customize the connection
                socket.Options.AddSubProtocol("wss");
                socket.Options.SetRequestHeader("Origin", "https://baskentenerji.com");
               

                try
                {
                    await socket.ConnectAsync(new Uri($"wss://stream.binance.com:9443/ws/{coinSymbol}@trade"), CancellationToken.None);

                    _coinSockets[coinSymbol] = socket;

                    // Start listening to messages
                    _ = Task.Run(async () => await ReceivePriceUpdates(socket, coinSymbol,""));
                }
                catch (WebSocketException ex)
                {
                    _logger.LogError(ex, $"WebSocket connection failed for {coinSymbol}");
                    // Optionally implement retry logic
                }
            }
        }
        public async Task StartTrackingCoin(string coinSymbol, string? exchange)
        {
           
            if (!_coinSockets.ContainsKey(coinSymbol))
            {
                var socket = new ClientWebSocket();
                // Add WebSocket options
                socket.Options.AddSubProtocol("wss");
                socket.Options.SetRequestHeader("Origin", "https://baskentenerji.com");
                try
                {
                    if (exchange == "binance" || string.IsNullOrEmpty(exchange))
                    {
                        // Binance WebSocket connection
                        await socket.ConnectAsync(new Uri($"wss://stream.binance.com:9443/ws/{coinSymbol}@trade"), CancellationToken.None);
                    }
                    else if (exchange == "mexc")
                    {
                        // MEXC WebSocket connection
                        await socket.ConnectAsync(new Uri("wss://wbs.mexc.com/ws"), CancellationToken.None);
                        // Subscribe to the stream after connecting
                        var subscriptionMessage = new
                        {
                            method = "SUBSCRIPTION",
                            @params = new[] { $"spot@public.deals.v3.api@{coinSymbol.ToUpper()}" },
                            id = 1
                        };
                        // Send subscription message
                        var subscriptionJson = JsonSerializer.Serialize(subscriptionMessage);
                        var subscriptionBytes = Encoding.UTF8.GetBytes(subscriptionJson);
                        await socket.SendAsync(new ArraySegment<byte>(subscriptionBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    _coinSockets[coinSymbol] = socket;
                    // Start listening to messages - pass exchange name
                    _ = Task.Run(async () => await ReceivePriceUpdates(socket, coinSymbol, exchange ?? "binance"));
                }
                catch (WebSocketException ex)
                {
                    _logger.LogError(ex, $"WebSocket connection failed for {coinSymbol} on {exchange}");
                    // Optionally implement retry logic
                }
            }
        }

    }
}
