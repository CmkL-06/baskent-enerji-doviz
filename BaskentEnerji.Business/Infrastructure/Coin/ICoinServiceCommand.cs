using BaskentEnerji.Entity.Modals.RequestModals.Coin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.Coin
{
    public interface ICoinServiceCommand
    {
        Task Admin_SaveCoin(rm_savecoin_admin data);
        Task Admin_DeleteCoin(Guid coinId);
        Task Admin_AddPair(rm_addpair data);
        Task Admin_DeletePair(Guid pairId);

        Task SaveTable(rm_savetable data);
        Task DeleteTable(Guid TableId);
        Task SaveCoin(rm_savecoin data);
        Task DeleteCoin(Guid coinId);
        Task SaveFavoriteCoin(rm_savefavoritecoin data);
        Task DeleteFavoriteCoin(Guid userFavCoinId);
    }
}
