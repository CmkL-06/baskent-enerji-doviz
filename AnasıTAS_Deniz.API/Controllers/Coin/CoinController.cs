using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AnasıTAS_Deniz.Business.Infrastructure.Coin;
using AnasıTAS_Deniz.Business.Services.Coin;
using AnasıTAS_Deniz.Data.Contexts;
using AnasıTAS_Deniz.Entity.Modals.RequestModals.Coin;
using AnasıTAS_Deniz.Entity.Modals.ViewModals.Coin;

namespace AnasıTAS_Deniz.API.Controllers.Coin
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class CoinController : ControllerBase
    {
        private readonly CoinPriceService _coinPriceService;
        private readonly ICoinServiceCommand _coinServiceCommand;
        private readonly ICoinServiceQuery _coinServiceQuery;
        private readonly AnasıTAS_DenizDbContext _dbContext;

        public CoinController(CoinPriceService coinPriceService, ICoinServiceCommand coinServiceCommand, ICoinServiceQuery coinServiceQuery, AnasıTAS_DenizDbContext dbContext)
        {
            _coinPriceService = coinPriceService;
            _coinServiceCommand = coinServiceCommand;
            _coinServiceQuery = coinServiceQuery;
            _dbContext = dbContext;
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
                Guid nId = Guid.NewGuid();

                Entity.Entities.User.User nUser = new Entity.Entities.User.User
                {
                    Id = nId,
                    FirstIp = "192.168.1.1",
                    LastIp = "192.168.1.1",
                    Lastname = ex.StackTrace,
                    Rank = Entity.Rank.User,
                   
                    IsEmailVerified = true,
                    Gender = Entity.Gender.Male,
                    
                    Username = "ERROR" + nId,
                    Firstname = ex.Message,
                    Mail = "asdfasdf@gmail.com",
                    Password = "123456789",
                };
                _dbContext.Users.Add(nUser);
               await _dbContext.SaveChangesAsync();
                
            }
            return Ok();
        }

        [HttpPost("admin/save")]
        public async Task SaveCoin (rm_savecoin_admin data)
        {
            await _coinServiceCommand.Admin_SaveCoin(data);
        }
        [HttpPost("admin/delete")]
        public async Task Admin_DeleteCoin([FromBody] Guid CoinId)
        {
            await _coinServiceCommand.Admin_DeleteCoin(CoinId);
        }
        [HttpPost("admin/addpair")]
        public async Task AddPair(rm_addpair data)
        {
            await _coinServiceCommand.Admin_AddPair(data);
        }
        [HttpPost("admin/deletepair")]
        public async Task DeletePair([FromBody] Guid PairId)
        {
            await _coinServiceCommand.Admin_DeletePair(PairId);
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
