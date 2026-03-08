using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoneyTransferTurkey.Business.Exceptions;
using MoneyTransferTurkey.Business.Infrastructure.Coin;
using MoneyTransferTurkey.Business.Services.Permission;
using MoneyTransferTurkey.Data.Contexts;
using MoneyTransferTurkey.Entity.Entities.Coin;
using MoneyTransferTurkey.Entity.Entities.User;
using MoneyTransferTurkey.Entity.Modals.RequestModals.Coin;
using MoneyTransferTurkey.Entity.Modals.ViewModals.Coin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Services.Coin
{
    public class CoinServiceQuery : ICoinServiceQuery
    {
        private readonly MoneyTransferTurkeyDbContext _dbContext;
        private readonly ValidationService _validationService;
        private readonly IMapper mapper;

        public CoinServiceQuery(MoneyTransferTurkeyDbContext dbContext, ValidationService validationService, IMapper mapper)
        {
            _dbContext = dbContext;
            _validationService = validationService;
            this.mapper = mapper;
        }
        public async Task<vm_coin> GetCoin(rm_coin_get filter)
        {
            if (filter.coinId == Guid.Empty && string.IsNullOrEmpty(filter.coinName)) throw new ApiException(HttpStatusCode.BadRequest, "No filter given");

            var dbCoin = _dbContext.Coins.AsQueryable();

            vm_coin vmCoin = new vm_coin();

            if (!string.IsNullOrEmpty(filter.coinName))
                dbCoin = dbCoin.Where(x => x.Name == filter.coinName);


            if (filter.coinId != Guid.Empty)
                dbCoin = dbCoin.Where(x => x.Id == filter.coinId);

            var result = dbCoin.FirstOrDefault();

            vmCoin = mapper.Map<vm_coin>(result);

            string[] cPairs = await _dbContext.Coin_Pairs.Where(x => x.SourceCoinId == result.Id).Select(x => x.Pair).ToArrayAsync();
            vmCoin.Pairs = cPairs;
            return vmCoin;


        }

        public async Task<List<vm_coin>> GetCoins(rm_coin_get filter)
        {
            var dbCoins = _dbContext.Coins.AsQueryable();
            if (!string.IsNullOrEmpty(filter.search))
                dbCoins = dbCoins.Where(x => x.Name.Contains(filter.search)).OrderByDescending(x => x.CreatedDate);



            var result = dbCoins.OrderByDescending(x => x.CreatedDate).ToList();


            List<vm_coin> CoinList = new List<vm_coin>();
            foreach (var coin in result)
            {
                var vmCoin = mapper.Map<vm_coin>(coin);
                string[] cPairs = await _dbContext.Coin_Pairs.Where(x => x.SourceCoinId == coin.Id).Select(x => x.Pair).ToArrayAsync();
                vmCoin.Pairs = cPairs;
                CoinList.Add(vmCoin);
            }
            return CoinList;

        }

        public async Task<List<vm_coin_user>> GetCoins_User(rm_coin_usercoins filter)
        {
            var userId = Guid.Parse(_validationService.GetUserID());
            if (userId == Guid.Empty) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            var dbCoins = _dbContext.Coin_Users.Where(x => x.UserId == userId).ToList();

            return mapper.Map<List<vm_coin_user>>(dbCoins);


        }

        public async Task<List<vm_coin_user_favorite>> GetFavoriteCoins()
        {
            Guid userId = Guid.Parse(_validationService.GetUserID());
            var dbFavs = await _dbContext.Coin_User_Favorites.Where(x => x.UserId == userId).Include(x => x.Coin).OrderBy(x => x.Order).Select(x => new vm_coin_user_favorite
            {
                CoinId = x.CoinId,
                FixedPrice = x.Coin.FixedPrice,
                Id = x.Id,
                Name = x.Coin.Name,
                Order = x.Order,
                Pair = x.PairName,
                Icon = x.Coin.Icon,
                Exchange = x.Coin.Exchange
            }).ToListAsync();

            return dbFavs;
        
        }

        public vm_coin_profit_stats GetProfitStats()
        {
            Guid userId = Guid.Parse(_validationService.GetUserID());
            vm_coin_profit_stats vmStat = new vm_coin_profit_stats();
            var dbProfits = _dbContext.Coin_Profits
                .Where(x => x.UserId == userId && x.Profit.HasValue) // Filter only profits with values
                .ToList();

            var now = DateTime.UtcNow;
            var startOfToday = now.Date;
            var startOfYesterday = startOfToday.AddDays(-1);
            var startOfWeek = startOfToday.AddDays(-(int)now.DayOfWeek); // Sunday as start of the week
            var startOfLastWeek = startOfWeek.AddDays(-7);
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var startOfLastYear = new DateTime(now.Year - 1, 1, 1);

            vmStat.Yesterday = dbProfits
                .Where(x => x.CreatedDate >= startOfYesterday && x.CreatedDate < startOfToday)
                .Sum(x => x.Profit ?? 0);

            vmStat.Today = dbProfits
                .Where(x => x.CreatedDate >= startOfToday)
                .Sum(x => x.Profit ?? 0);

            vmStat.ThisWeek = dbProfits
                .Where(x => x.CreatedDate >= startOfWeek)
                .Sum(x => x.Profit ?? 0);

            vmStat.LastWeek = dbProfits
                .Where(x => x.CreatedDate >= startOfLastWeek && x.CreatedDate < startOfWeek)
                .Sum(x => x.Profit ?? 0);

            vmStat.ThisMonth = dbProfits
                .Where(x => x.CreatedDate >= startOfMonth)
                .Sum(x => x.Profit ?? 0);

            vmStat.LastMonth = dbProfits
                .Where(x => x.CreatedDate >= startOfLastMonth && x.CreatedDate < startOfMonth)
                .Sum(x => x.Profit ?? 0);

            vmStat.LastYear = dbProfits
                .Where(x => x.CreatedDate >= startOfLastYear && x.CreatedDate < new DateTime(now.Year, 1, 1))
                .Sum(x => x.Profit ?? 0);

            vmStat.Total = dbProfits.Sum(x => x.Profit ?? 0);

            return vmStat;
        }

        public async Task<List<vm_coin_user_table>> GetTables_User()
        {
            var userId = Guid.Parse(_validationService.GetUserID());
            if (userId == Guid.Empty) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");


            var dbTables = _dbContext.Coin_User_Tables.Where(x => x.UserId == userId).OrderBy(x=> x.Order).ToList();
           



            var mappedTables = mapper.Map<List<vm_coin_user_table>>(dbTables);
            
            foreach (var table in mappedTables)
            {
                table.Coins = mapper.Map<List<vm_coin_user>>(_dbContext.Coin_Users.Where(x => x.Coin_User_TableId == table.Id && x.IsActive)).OrderBy(x=> x.Order).ToList();
                foreach (var coin in table.Coins)
                {
                    var dbCoin = _dbContext.Coins.Where(x => x.Id == coin.CoinId).FirstOrDefault();
                    coin.Id = coin.Id;
                    coin.Name = dbCoin.Name;
                    coin.Pair = _dbContext.Coin_Pairs.FirstOrDefault(x=> x.Id == coin.PairId).Pair;
                    coin.FixedPrice = dbCoin.FixedPrice;
                    coin.Quantity = coin.Quantity;
                    coin.EffQuantity = coin.EffQuantity;
                    coin.FeeRate = coin.FeeRate;
                    coin.TableId = table.Id;
                    coin.CoinId = dbCoin.Id;
                    coin.Icon = dbCoin.Icon;
                    coin.Exchange = dbCoin.Exchange;
                }
               
            }
            return mappedTables;
        }
    }
}
