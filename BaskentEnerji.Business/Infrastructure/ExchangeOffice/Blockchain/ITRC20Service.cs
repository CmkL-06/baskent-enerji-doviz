using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.ExchangeOffice.Blockchain
{
    public interface ITRC20Service
    {
        Task<object> GetRecentDepositsAsync(string walletAddress = null);
    }
}