using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MoneyTransferTurkey.Business.Exceptions;
using MoneyTransferTurkey.Business.Infrastructure.Coin;
using MoneyTransferTurkey.Business.Services.Permission;
using MoneyTransferTurkey.Data.Contexts;
using MoneyTransferTurkey.Entity.Entities.Coin;
using MoneyTransferTurkey.Entity.Modals.RequestModals.Coin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MoneyTransferTurkey.Business.Services.Coin
{
    public class CoinServiceCommand : ICoinServiceCommand
    {
        private readonly MoneyTransferTurkeyDbContext _dbContext;
        private readonly ValidationService _validationService;
        private readonly IMapper mapper;

        public CoinServiceCommand(MoneyTransferTurkeyDbContext dbContext, ValidationService validationService, IMapper mapper)
        {
            _dbContext = dbContext;
            _validationService = validationService;
            this.mapper = mapper;
        }

        public async Task Admin_SaveCoin(rm_savecoin_admin data)
        {
            if (!await _validationService.IsStaff())
                throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            var dbCoin = await _dbContext.Coins.FirstOrDefaultAsync(x => x.Id == data.Id);

            if (dbCoin != null) // Existing coin
            {
                // Retrieve all existing pairs for the coin
                var existingPairs = await _dbContext.Coin_Pairs
                    .Where(x => x.SourceCoinId == dbCoin.Id)
                    .ToListAsync();

                // Identify new pairs to add
                var newPairNames = data.Pairs.Except(existingPairs.Select(x => x.Pair)).ToList();

                var newPairs = newPairNames.Select(name => new Coin_Pair
                {
                    Pair = name,
                    SourceCoinId = dbCoin.Id
                });

                // Identify pairs to remove
                var pairsToRemove = existingPairs.Where(x => !data.Pairs.Contains(x.Pair)).ToList();

                // Update coin properties and save changes
                _dbContext.Entry(dbCoin).CurrentValues.SetValues(data);

                // Perform pair updates
                _dbContext.Coin_Pairs.AddRange(newPairs);
                _dbContext.Coin_Pairs.RemoveRange(pairsToRemove);
            }
            else // New coin
            {
                var newCoin = new Entity.Entities.Coin.Coin
                {
                    Id = data.Id,
                    ShortDescription = data.ShortDescription,
                    Cover = data.Cover,
                    Description = data.Description,
                    FixedPrice = data.FixedPrice,
                    Icon = data.Icon,
                    IsActive = data.IsActive,
                    Name = data.Name,
                    Exchange = data.Exchange

                };

                await _dbContext.Coins.AddAsync(newCoin);
                await _dbContext.SaveChangesAsync(); // Save new coin to generate ID

                var pairs = data.Pairs.Select(name => new Coin_Pair
                {
                    Pair = name,
                    SourceCoinId = newCoin.Id
                });

                await _dbContext.Coin_Pairs.AddRangeAsync(pairs);
            }

            await _dbContext.SaveChangesAsync();
        }


        public async Task Admin_DeleteCoin(Guid coinId)
        {
            if (!await _validationService.IsAdminAsync()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
            await _dbContext.Coins.Where(x => x.Id == coinId).ExecuteDeleteAsync();
        }

        public async Task Admin_AddPair(rm_addpair data)
        {
            if (!await _validationService.IsAdminAsync()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
            await _dbContext.Coin_Pairs.AddAsync(mapper.Map<Coin_Pair>(data));
            await _dbContext.SaveChangesAsync();
        }

        public async Task Admin_DeletePair(Guid pairId)
        {
            if (!await _validationService.IsAdminAsync()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
            await _dbContext.Coin_Pairs.Where(x => x.Id == pairId).ExecuteDeleteAsync();
        }

        public async Task SaveTable(rm_savetable data)
        {
            var dbTable = _dbContext.Coin_User_Tables.FirstOrDefault(x => x.Id == data.Id);
            if (dbTable == null)
            {
                Coin_User_Table nTable = new Coin_User_Table
                {
                    Name = data.Name,
                    Order = data.Order,
                    Id = data.Id,
                    UserId = Guid.Parse(_validationService.GetUserID())
                };
                _dbContext.Coin_User_Tables.Add(nTable);

            }
            else
            {
                if (!await _validationService.HasPermissionAsync(dbTable.UserId)) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

                if (data.delete)
                {
                    _dbContext.Coin_User_Tables.Remove(dbTable);

                }
                else _dbContext.Entry(dbTable).CurrentValues.SetValues(data);

            }



            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteTable(Guid TableId)
        {
            var dbTable = _dbContext.Coin_User_Tables.FirstOrDefault(x => x.Id == TableId);
            if (dbTable == null) throw new ApiException(HttpStatusCode.NotFound, "Table couldn't be found.");
            if (await _validationService.HasPermissionAsync(dbTable.UserId)) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            _dbContext.Coin_User_Tables.Remove(dbTable);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveCoin(rm_savecoin data)
        {
            if (data.Coin_User_TableId == null) throw new ApiException(HttpStatusCode.BadRequest, "Provide a table ID.");
            var dbTable = _dbContext.Coin_User_Tables.FirstOrDefault(x => x.Id == data.Coin_User_TableId);
            if (dbTable == null) throw new ApiException(HttpStatusCode.NotFound, "Table couldn't be found.");
            if (!await _validationService.HasPermissionAsync(dbTable.UserId)) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");


            if (_dbContext.Coin_Pairs.FirstOrDefault(x => x.Pair == data.PairName) == null) throw new ApiException(HttpStatusCode.NotFound, "Pair couldn't be found");
            var nCoinData = mapper.Map<Coin_User>(data);
            vm_effdata effData = new vm_effdata();
            effData = GetEffectiveCostAndQuantity(nCoinData.BuyPrice, nCoinData.Quantity, nCoinData.FeeRate);
            var existingData = _dbContext.Coin_Users.FirstOrDefault(x => x.Id == nCoinData.Id);
            if (existingData != null)
            {
                if (nCoinData.IsActive = false && nCoinData.SellPrice == null) throw new ApiException(HttpStatusCode.BadRequest, "You must provide sell price.");

                decimal feePercent = nCoinData.FeeRate / 100;
                if (existingData.IsActive && !data.IsActive) // sell coin
                {

                    decimal profit = new decimal(0);

                    decimal sellCost = (data.SellPrice ?? 0) * effData.EffQuantity;
                    decimal effSellCost = (sellCost) * (1 - feePercent);
                    profit = effSellCost - effData.BuyCost;
                    existingData.Profit = profit;
                    existingData.IsActive = false;
                    existingData.SellPrice = data.SellPrice;

                    Coin_Profit nProfit = new Coin_Profit
                    {
                        Id = Guid.NewGuid(),
                        SellPrice = existingData.SellPrice,
                        BuyPrice = existingData.BuyPrice,
                        CoinId = existingData.CoinId,
                        EffQuantity = existingData.EffQuantity,
                        FeeRate = existingData.FeeRate,
                        PairId = existingData.PairId,
                        Profit = profit,
                        Quantity = existingData.Quantity,
                        UserId = existingData.UserId,

                    };
                    _dbContext.Coin_Profits.Add(nProfit);

                    if (existingData.EffQuantity - nCoinData.Quantity > 0)
                    {
                        Coin_User newCoin = new Coin_User
                        {
                            UserId = Guid.Parse(_validationService.GetUserID()),
                            Id = Guid.NewGuid(), // Ensure a unique ID
                            Order = existingData.Order,
                            Quantity = (existingData.EffQuantity - nCoinData.Quantity) * (1 + feePercent),
                            EffQuantity = GetEffectiveCostAndQuantity(nCoinData.BuyPrice, (existingData.EffQuantity - nCoinData.Quantity) * (1 + feePercent), nCoinData.FeeRate).EffQuantity,
                            IsActive = true,
                            SellPrice = 0,
                            BuyPrice = existingData.BuyPrice,
                            CoinId = existingData.CoinId,
                            Coin_User_TableId = existingData.Coin_User_TableId,
                            FeeRate = existingData.FeeRate,
                            CreatedDate = existingData.CreatedDate,
                            PairId = existingData.PairId,
                            Profit = 0,
                        };


                       
                        newCoin.UserId = Guid.Parse(_validationService.GetUserID());

                        newCoin.Id = Guid.NewGuid();
                        newCoin.Order = existingData.Order;

                        decimal remainsQuantiy = existingData.EffQuantity - nCoinData.Quantity;
                        vm_effdata effData2 = new vm_effdata();
                        newCoin.Quantity = remainsQuantiy * (1 + feePercent);
                        effData2 = GetEffectiveCostAndQuantity(nCoinData.BuyPrice, newCoin.Quantity, nCoinData.FeeRate);
                        newCoin.EffQuantity = effData2.EffQuantity;

                       
                        newCoin.IsActive = true;

                        _dbContext.Coin_Users.Add(newCoin);
                      
                        await _dbContext.SaveChangesAsync();

                    }

                    _dbContext.Coin_Users.Remove(existingData);
                    await _dbContext.SaveChangesAsync();
                }
                else // savecoin
                {

                    existingData.Quantity = data.Quantity;
                    existingData.EffQuantity = effData.EffQuantity;
                    existingData.BuyPrice = data.BuyPrice;
                    nCoinData.UserId = Guid.Parse(_validationService.GetUserID());
                    nCoinData.IsActive = true;
                    var dbPair = _dbContext.Coin_Pairs.FirstOrDefault(x => x.Pair == data.PairName);
                    if (dbPair == null) throw new ApiException(HttpStatusCode.NotFound, "Pair couldn't be found.");
                    _dbContext.Entry(existingData).CurrentValues.SetValues(nCoinData);
                    existingData.PairId = dbPair.Id;


                }

            }
            else //new coin
            {

                nCoinData.UserId = Guid.Parse(_validationService.GetUserID());
                nCoinData.PairId = _dbContext.Coin_Pairs.FirstOrDefault(x => x.Pair == data.PairName).Id;
                nCoinData.IsActive = true;
                nCoinData.EffQuantity = effData.EffQuantity;


                _dbContext.Coin_Users.Add(nCoinData);
            }

            await _dbContext.SaveChangesAsync();
        }

        private vm_effdata GetEffectiveCostAndQuantity(decimal buyprice, decimal quantity, decimal feerate)
        {

            decimal feePercent = feerate / 100;
            decimal profit = new decimal(0);
            decimal buyCost = buyprice * quantity;
            decimal effBuyCost = buyCost * (1 + feePercent);
            decimal effQuantity = quantity * (1 - feePercent);

            vm_effdata vmData = new vm_effdata
            {
                BuyCost = effBuyCost,
                EffQuantity = effQuantity,
            };
            return vmData;


        }

        public async Task DeleteCoin(Guid coinId)
        {
            var dbCoin = _dbContext.Coin_Users.FirstOrDefault(x => x.Id == coinId);
            if (dbCoin == null) throw new ApiException(HttpStatusCode.NotFound, "Coin couldn't be found.");
            if (!await _validationService.HasPermissionAsync(dbCoin.UserId)) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            _dbContext.Coin_Users.Remove(dbCoin);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveFavoriteCoin(rm_savefavoritecoin data)
        {
            var dbCoin = _dbContext.Coin_User_Favorites.FirstOrDefault(x => x.Id == data.Id);
              Guid userId = Guid.Parse( _validationService.GetUserID());
            var mappedData = mapper.Map<Coin_User_Favorite>(data);
            mappedData.UserId = userId;
            if (dbCoin != null)
            {
                if (!await _validationService.HasPermissionAsync(dbCoin.UserId)) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
                _dbContext.Entry(dbCoin).CurrentValues.SetValues(mappedData);
               
            } else
            {
                mappedData.UserId = userId;
                await _dbContext.Coin_User_Favorites.AddAsync(mappedData);
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteFavoriteCoin(Guid userFavCoinId)
        {

            var dbCoin = _dbContext.Coin_User_Favorites.FirstOrDefault(x => x.Id == userFavCoinId);
            if (dbCoin == null) throw new ApiException(HttpStatusCode.NotFound, "Coin couldn't be found.");
            if (!await _validationService.HasPermissionAsync(dbCoin.UserId)) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            _dbContext.Coin_User_Favorites.Remove(dbCoin);
            await _dbContext.SaveChangesAsync();
        }

        private class vm_effdata
        {
            public decimal BuyCost;
            public decimal EffQuantity;
        }
    }
}
