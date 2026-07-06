using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BaskentEnerji.Business.Infrastructure.Coin;
using BaskentEnerji.Business.Services.Coin;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Modals.RequestModals.Coin;
using BaskentEnerji.Entity.Modals.ViewModals.Coin;
using BaskentEnerji.Business.Services.Permission;

namespace BaskentEnerji.API.Controllers.Coin
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class CoinController : ControllerBase
    {
        private readonly CoinPriceService _coinPriceService;
        private readonly ICoinServiceCommand _coinServiceCommand;
        private readonly ICoinServiceQuery _coinServiceQuery;
        private readonly BaskentEnerjiDbContext _dbContext;
        private readonly ValidationService _validationService;

        public CoinController(CoinPriceService coinPriceService, ICoinServiceCommand coinServiceCommand, ICoinServiceQuery coinServiceQuery, BaskentEnerjiDbContext dbContext, ValidationService validationService)
        {
            _coinPriceService = coinPriceService;
            _coinServiceCommand = coinServiceCommand;
            _coinServiceQuery = coinServiceQuery;
            _dbContext = dbContext;
            _validationService = validationService;
        }

        [HttpGet("track/{coinSymbol}")]
        [AllowAnonymous]
        public async Task<IActionResult> TrackCoin(string coinSymbol)
        {
            // await _coinPriceService.StartTrackingCoin(coinSymbol);
           
            var dbCoins = _dbContext.Coins.ToList();
            try
            {
                foreach (var coin in dbCoins)
                {
                    string coinName = coin.Name.ToLower();
                    coinName = coinName.Replace("ı", "i").Replace("İ", "i").Replace("I", "i");
                    await _coinPriceService.StartTrackingCoin(coinName + "usdt", coin.Exchange ?? "binance");
                }
                // return Ok($"Started tracking {coinSymbol}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CoinController] TrackCoin error: {ex.Message}\n{ex.StackTrace}");
            }
            return Ok();
        }

        [HttpPost("admin/save")]
        public async Task<IActionResult> SaveCoin (rm_savecoin_admin data)
        {
            if (!await _validationService.IsAdminAsync()) return Forbid();
            await _coinServiceCommand.Admin_SaveCoin(data);
            return Ok();
        }
        [HttpPost("admin/delete")]
        public async Task<IActionResult> Admin_DeleteCoin([FromBody] Guid CoinId)
        {
            if (!await _validationService.IsAdminAsync()) return Forbid();
            await _coinServiceCommand.Admin_DeleteCoin(CoinId);
            return Ok();
        }
        [HttpPost("admin/addpair")]
        public async Task<IActionResult> AddPair(rm_addpair data)
        {
            if (!await _validationService.IsAdminAsync()) return Forbid();
            await _coinServiceCommand.Admin_AddPair(data);
            return Ok();
        }
        [HttpPost("admin/deletepair")]
        public async Task<IActionResult> DeletePair([FromBody] Guid PairId)
        {
            if (!await _validationService.IsAdminAsync()) return Forbid();
            await _coinServiceCommand.Admin_DeletePair(PairId);
            return Ok();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<List<vm_coin>> GetCoins([FromQuery]rm_coin_get filter)
        {
           return await _coinServiceQuery.GetCoins(filter);
        }

        [HttpGet("filter")]
        [AllowAnonymous]
        public async Task<vm_coin> GetCoin([FromQuery] rm_coin_get filter)
        {
            return await _coinServiceQuery.GetCoin(filter);
        }

        [HttpGet("user")]
        public async Task<List<vm_coin_user>> GetCoins_User([FromQuery]rm_coin_usercoins filter)
        {
           return await _coinServiceQuery.GetCoins_User(filter);
        }

        [HttpGet("user/tables")]
        public async Task<List<vm_coin_user_table>> GetTables_User()
        {
            return await _coinServiceQuery.GetTables_User();
        }

        [HttpPost("user/table")]
        public async Task SaveTable(rm_savetable data)
        {
            await _coinServiceCommand.SaveTable(data);
        }
        [HttpPost("user/table/delete")]
        public async Task DeleteTable([FromBody]Guid TableId)
        {
            await _coinServiceCommand.DeleteTable(TableId);
        }
        [HttpPost("user/coin")]
        public async Task SaveCoin(rm_savecoin data)
        {
            await _coinServiceCommand.SaveCoin(data);
        }

        [HttpPost("user/coin/delete")]
        public async Task DeleteCoin([FromBody]Guid coinId)
        {
            await _coinServiceCommand.DeleteCoin(coinId);
        }

        [HttpGet("user/profitstats")]
        public vm_coin_profit_stats GetStats()
        {
            return  _coinServiceQuery.GetProfitStats();
        }

        [HttpGet("user/favorites")]
        public async Task<List<vm_coin_user_favorite>> GetFavoriteCoins()
        {
            return await _coinServiceQuery.GetFavoriteCoins();
        }
        [HttpPost("user/favorite")]
        public async Task SaveFavoriteCoin(rm_savefavoritecoin data)
        {
             await _coinServiceCommand.SaveFavoriteCoin(data);
        }
    }
}
