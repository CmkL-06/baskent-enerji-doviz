using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Infrastructure.ExchangeOffice.Blockchain
{
    public interface ITRC20Service
    {
        Task<object> GetRecentDepositsAsync(string walletAddress = null);
    }
}