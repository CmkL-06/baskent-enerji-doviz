using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Infrastructure.Coin
{
    public interface ICoinPriceService
    {
        Task StartTrackingCoin(string coinSymbol);
        Task ReceivePriceUpdates(WebSocket socket, string coinSymbol);

    }
}
