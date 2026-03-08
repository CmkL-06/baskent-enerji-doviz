using BaskentEnerji.Entity.Modals.RequestModals.Coin;
using BaskentEnerji.Entity.Modals.ViewModals.Coin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.Coin
{
    public interface ICoinServiceQuery
    {
        Task<List<vm_coin>> GetCoins(rm_coin_get filter);
        Task<vm_coin> GetCoin(rm_coin_get filter);

        Task<List<vm_coin_user>> GetCoins_User(rm_coin_usercoins filter);
        Task<List<vm_coin_user_table>> GetTables_User();
        vm_coin_profit_stats GetProfitStats();
        Task<List<vm_coin_user_favorite>> GetFavoriteCoins();
    }
}
