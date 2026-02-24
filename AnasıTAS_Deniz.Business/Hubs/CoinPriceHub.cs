using Google.Apis.Logging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Hubs
{
    public class CoinPriceHub : Hub
    {
        private readonly ILogger<CoinPriceHub> _logger;

        public CoinPriceHub(ILogger<CoinPriceHub> logger)
        {
            _logger = logger;
        }
        public async Task SubscribeToCoin(string coinSymbol)
        {
            _logger.LogDebug("Subscribing client {ConnectionId} to coin group: {CoinSymbol}", Context.ConnectionId, coinSymbol);
            await Groups.AddToGroupAsync(Context.ConnectionId, coinSymbol);
        }


        public async Task UnsubscribeFromCoin(string coinSymbol)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, coinSymbol);
        }
    }
}
