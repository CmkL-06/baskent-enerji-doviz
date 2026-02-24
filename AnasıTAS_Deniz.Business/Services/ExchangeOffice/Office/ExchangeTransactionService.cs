using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.Client;
using AnasıTAS_Deniz.Business.Exceptions;
using AnasıTAS_Deniz.Business.Infrastructure.ExchangeOffice.Office;
using AnasıTAS_Deniz.Business.Services.ExchangeOffice.Party;
using AnasıTAS_Deniz.Business.Services.Permission;
using AnasıTAS_Deniz.Data.Contexts;
using AnasıTAS_Deniz.Entity;
using AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Currency;
using AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Office;
using AnasıTAS_Deniz.Entity.Modals.RequestModals.ExchangeService.Office;
using AnasıTAS_Deniz.Entity.Modals.RequestModals.Site.Reports;
using AnasıTAS_Deniz.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using AnasıTAS_Deniz.Entity.Modals.ViewModals.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AnasıTAS_Deniz.Business.Services.ExchangeOffice.Office
{
    public class ExchangeTransactionService : IExchangeTransactionService
    {
        private readonly AnasıTAS_DenizDbContext _context;
        private readonly IMapper _mapper;
        private readonly IVaultService _vaultService;
        private readonly ValidationService _validationService;
        private readonly PartyTransactionIntegration _partyIntegration;
        private readonly IMemoryCache _memoryCache;

        public ExchangeTransactionService(
            AnasıTAS_DenizDbContext context,
            IMapper mapper,
            IVaultService vaultService,
            ValidationService validationService,
            PartyTransactionIntegration partyIntegration, IMemoryCache memoryCache
            )
        {
            _context = context;
            _mapper = mapper;
            _vaultService = vaultService;
            _validationService = validationService;
            _partyIntegration = partyIntegration;
            _memoryCache = memoryCache;
        }

        public async Task<List<vm_exchangetransaction>> ProcessExchangeAsync(List<rm_exchangetransaction> request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validate input
                if (request == null || !request.Any())
                    throw new ArgumentException("No exchange transactions provided");

                // Get vault to determine office (using first request's vault - assuming all use same vault)
                var vaultId = request.First().VaultId;
                var vault = await _context.Vaults
                    .Include(v => v.Office)
                    .FirstOrDefaultAsync(v => v.Id == vaultId);

                if (vault == null)
                    throw new InvalidOperationException("Vault not found");

                // Result list to return
                var results = new List<vm_exchangetransaction>();

                // Create main transaction
                var exchangeTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    TransactionNumber = GenerateTransactionNumber(),
                    VaultId = vaultId,
                    CustomerId = request.First().CustomerId,
                    PartyId = request.First().PartyId,
                    UserId = Guid.Parse(_validationService.GetUserID()),
                    Type = TransactionType.Exchange,
                    TransactionDate = DateTime.UtcNow,
                    Status = TransactionStatus.Pending,
                    Notes = request.First().Notes,
                    CreatedDate = DateTime.UtcNow,
                    Details = new List<TransactionDetail>(),
                    Profit = 0, // Will accumulate
                    IsCustomRate = request.Any(x => x.CustomRate > 0),
                    IsDeleted = false,
                    DeletedReason = null,
                    deletedByUserId = null
                };

                decimal totalProfit = 0;

                // Process each exchange request
                foreach (var singleRequest in request)
                {
                    // Get exchange rate for this specific exchange
                    var exchangeRate = await GetExchangeRateAsync(
                        vault.OfficeId,
                        singleRequest.SourceCurrencyId,
                        singleRequest.TargetCurrencyId
                    );

                    if (exchangeRate == null)
                        throw new InvalidOperationException($"Exchange rate not found for {singleRequest.SourceCurrencyId} to {singleRequest.TargetCurrencyId}");

                    // Calculate amounts for this exchange
                    var rate = singleRequest.CustomRate ??
                        (singleRequest.IsBuyingFromCustomer ? exchangeRate.BuyRate : exchangeRate.SellRate);
                    var targetAmount = singleRequest.SourceAmount * rate;
                    var netTargetAmount = targetAmount; // No commission deduction

                    // Calculate profit for this exchange (directly using buy/sell rate)
                    decimal profit = 0;

                    if (singleRequest.IsBuyingFromCustomer)
                    {
                        // Office buys from customer (at BuyRate or custom) and could sell at SellRate
                        profit = (exchangeRate.SellRate - rate) * singleRequest.SourceAmount;
                    }
                    else
                    {
                        // Office sells to customer (at SellRate or custom) and could buy at BuyRate
                        profit = (rate - exchangeRate.BuyRate) * singleRequest.SourceAmount;
                    }

                    totalProfit += profit;

                    // Create transaction details for this exchange
                    if (singleRequest.IsBuyingFromCustomer)
                    {
                        // Customer sells to us: we debit target currency, credit source currency
                        exchangeTransaction.Details.Add(new TransactionDetail
                        {
                            Id = Guid.NewGuid(),
                            CurrencyId = singleRequest.TargetCurrencyId,
                            Side = TransactionSide.Debit,
                            Amount = targetAmount,
                            Rate = rate,
                            Commission = 0,
                            NetAmount = netTargetAmount,
                            CreatedDate = DateTime.UtcNow,
                            ActualBuyRate = exchangeRate.BuyRate,
                            ActualSellRate = exchangeRate.SellRate,
                            CustomRate = singleRequest.CustomRate
                        });

                        exchangeTransaction.Details.Add(new TransactionDetail
                        {
                            Id = Guid.NewGuid(),
                            CurrencyId = singleRequest.SourceCurrencyId,
                            Side = TransactionSide.Credit,
                            Amount = singleRequest.SourceAmount,
                            Rate = rate,
                            Commission = 0,
                            NetAmount = singleRequest.SourceAmount,
                            CreatedDate = DateTime.UtcNow,
                            ActualBuyRate = exchangeRate.BuyRate,
                            ActualSellRate = exchangeRate.SellRate,
                            CustomRate = singleRequest.CustomRate
                        });
                    }
                    else
                    {
                        // Customer buys from us: we credit target currency, debit source currency
                        exchangeTransaction.Details.Add(new TransactionDetail
                        {
                            Id = Guid.NewGuid(),
                            CurrencyId = singleRequest.SourceCurrencyId,
                            Side = TransactionSide.Debit,
                            Amount = singleRequest.SourceAmount,
                            Rate = rate,
                            Commission = 0,
                            NetAmount = singleRequest.SourceAmount,
                            CreatedDate = DateTime.UtcNow,
                            ActualBuyRate = exchangeRate.BuyRate,
                            ActualSellRate = exchangeRate.SellRate,
                            CustomRate = singleRequest.CustomRate
                        });

                        exchangeTransaction.Details.Add(new TransactionDetail
                        {
                            Id = Guid.NewGuid(),
                            CurrencyId = singleRequest.TargetCurrencyId,
                            Side = TransactionSide.Credit,
                            Amount = targetAmount,
                            Rate = rate,
                            Commission = 0,
                            NetAmount = netTargetAmount,
                            CreatedDate = DateTime.UtcNow,
                            ActualBuyRate = exchangeRate.BuyRate,
                            ActualSellRate = exchangeRate.SellRate,
                            CustomRate = singleRequest.CustomRate
                        });
                    }

                    // Add result for this exchange
                    results.Add(new vm_exchangetransaction
                    {
                        SourceCurrencyId = singleRequest.SourceCurrencyId,
                        TargetCurrencyId = singleRequest.TargetCurrencyId,
                        SourceAmount = singleRequest.SourceAmount,
                        TargetAmount = targetAmount,
                        AppliedRate = rate,
                        Commission = 0,
                        ProfitLoss = profit
                    });
                }

                // Set total profit on transaction
                exchangeTransaction.Profit = totalProfit;

                // Save transaction
                _context.Transactions.Add(exchangeTransaction);
                await _context.SaveChangesAsync();

                List<Transaction> dbTss = new List<Transaction>(); dbTss =  await _context.Transactions.Where(x => x.TransactionNumber == exchangeTransaction.TransactionNumber).Include(x=> x.Details).ToListAsync();
                // Update vault balances

          
                foreach (var detail in exchangeTransaction.Details)
                {
                    var amount = detail.Side == TransactionSide.Credit
                        ? detail.NetAmount
                        : -detail.Amount;

                    var updateData = new rm_updatevaultbalance
                    {
                        amount = amount,
                        currencyId = detail.CurrencyId,
                        description = $"Exchange transaction {exchangeTransaction.TransactionNumber}",
                        vaultId = vaultId,
                        TransactionType = TransactionType.Exchange
                    };

                    await _vaultService.UpdateVaultBalanceAsync(updateData);
                }

                // Update transaction status
                exchangeTransaction.Status = TransactionStatus.Completed;
                await _context.SaveChangesAsync();

                // Create party account entries if party is specified
                if (exchangeTransaction.PartyId.HasValue)
                {
                    await _partyIntegration.CreatePartyAccountEntriesAsync(exchangeTransaction);
                }

                // Commit transaction
                await transaction.CommitAsync();
                
                // Invalidate Z-report cache AFTER transaction commit
                InvalidateZReportCache(vault.OfficeId, DateTime.Today);
                InvalidateZReportCache(vault.OfficeId, exchangeTransaction.TransactionDate);
                InvalidateZReportCache(vault.OfficeId, DateTime.Now.Date);

                return results;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task<Transaction> TransferBetweenVaultsAsync(rm_transferbetweenvaults request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Verify source vault has sufficient balance
                var hasBalance = await _vaultService.CheckVaultBalanceAsync(
                    request.SourceVaultId,
                    request.CurrencyId,
                    request.Amount
                );

                if (!hasBalance)
                    throw new InvalidOperationException("Insufficient balance in source vault");

                // Create transfer transaction
                var transferTransaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    TransactionNumber = GenerateTransactionNumber(),
                    VaultId = request.SourceVaultId,
                    UserId = GetCurrentUserId(),
                    Type = TransactionType.Transfer,
                    TransactionDate = DateTime.UtcNow,
                    Status = TransactionStatus.Pending,
                    Notes = $"Transfer to vault {request.TargetVaultId}: {request.Notes}",
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = false,
                    DeletedReason = null,
                    deletedByUserId = null,
                    Profit = 0,
                    IsCustomRate = false,
                    Details = new List<TransactionDetail>
                    {
                        new TransactionDetail
                        {
                            Id = Guid.NewGuid(),
                            CurrencyId = request.CurrencyId,
                            Side = TransactionSide.Debit,
                            Amount = request.Amount,
                            Rate = 1,
                            Commission = 0,
                            NetAmount = request.Amount,
                            CreatedDate = DateTime.UtcNow
                        }
                    }
                };

                _context.Transactions.Add(transferTransaction);

                var nDataSource = new rm_updatevaultbalance
                {
                    amount = -request.Amount,
                    currencyId = request.CurrencyId,
                    description = null,
                    vaultId = request.SourceVaultId,
                };
                var nDataTarget = new rm_updatevaultbalance
                {
                    amount = request.Amount,
                    currencyId = request.CurrencyId,
                    vaultId = request.TargetVaultId,
                    description = null,
                };

                // Update balances
                await _vaultService.UpdateVaultBalanceAsync(
                   nDataSource
                );

                await _vaultService.UpdateVaultBalanceAsync(
                    nDataTarget
                );

                transferTransaction.Status = TransactionStatus.Completed;
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();
                
                // Invalidate Z-report cache AFTER transaction commit
                var sourceVault = await _context.Vaults.FirstOrDefaultAsync(v => v.Id == request.SourceVaultId);
                var targetVault = await _context.Vaults.FirstOrDefaultAsync(v => v.Id == request.TargetVaultId);
                
                if (sourceVault != null)
                {
                    InvalidateZReportCache(sourceVault.OfficeId, DateTime.Today);
                    InvalidateZReportCache(sourceVault.OfficeId, transferTransaction.TransactionDate);
                    InvalidateZReportCache(sourceVault.OfficeId, DateTime.Now.Date);
                }
                
                if (targetVault != null && targetVault.OfficeId != sourceVault?.OfficeId)
                {
                    InvalidateZReportCache(targetVault.OfficeId, DateTime.Today);
                    InvalidateZReportCache(targetVault.OfficeId, transferTransaction.TransactionDate);
                    InvalidateZReportCache(targetVault.OfficeId, DateTime.Now.Date);
                }

                return transferTransaction;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<decimal> CalculateProfitLossAsync(Guid officeId, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Transactions
                .Include(t => t.Vault)
                .Where(t =>
                    t.Vault.OfficeId == officeId &&
                    t.Status == TransactionStatus.Completed &&
                    t.Type == TransactionType.Exchange);

            if (startDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate <= endDate.Value);
            }

            var transactions = await query.ToListAsync();

            // Sum up the profit from all exchange transactions
            decimal totalProfitLoss = transactions.Sum(t => t.Profit);

            return totalProfitLoss;
        }

        public async Task<List<vm_transaction>> GetTransactionHistoryAsync(
      Guid? vaultId,
      DateTime? startDate,
      DateTime? endDate)
        {
            //var isAdmin = await _validationService.IsAdminAsync();
            var query = _context.Transactions
                .AsNoTracking()
                .Include(t => t.Vault)
                .Include(t => t.User)
                .Include(t => t.Details)
                    .ThenInclude(d => d.Currency)
                .AsSplitQuery() // PERFORMANCE FIX: Avoid cartesian explosion
                .AsQueryable();
            // .Where( t=> isAdmin || !t.IsDeleted );



            if (vaultId.HasValue)
                query = query.Where(t => t.VaultId == vaultId.Value);

            if (startDate.HasValue)
                query = query.Where(t => t.TransactionDate >= startDate.Value.Date);

            if (endDate.HasValue)
            {
                // Add 1 day and use < instead of <=
                var endOfDay = endDate.Value.Date.AddDays(1);
                query = query.Where(t => t.TransactionDate < endOfDay);
            }

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .Take(500) // PERFORMANCE FIX: Limit to prevent massive loads
                .ToListAsync();

            var mappedTransactions = _mapper.Map<List<vm_transaction>>(transactions);

            // PERFORMANCE FIX: Create dictionary for O(1) lookup instead of O(n²)
            var transactionDict = transactions.ToDictionary(t => t.Id);

            // Load deleted by usernames for transactions that were deleted
            var deletedTransactionIds = transactions
                .Where(t => t.deletedByUserId.HasValue)
                .Select(t => t.deletedByUserId.Value)
                .Distinct()
                .ToList();

            if (deletedTransactionIds.Any())
            {
                var deletedByUsers = await _context.Users
                    .AsNoTracking()
                    .Where(u => deletedTransactionIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.Username);

                foreach (var transaction in mappedTransactions)
                {
                    // PERFORMANCE FIX: Use dictionary O(1) lookup instead of FirstOrDefault O(n)
                    if (transactionDict.TryGetValue(transaction.Id, out var sourceTransaction) &&
                        sourceTransaction.deletedByUserId != null &&
                        deletedByUsers.TryGetValue(sourceTransaction.deletedByUserId.Value, out var username))
                    {
                        transaction.DeletedBy = username;
                    }
                }
            }
            
            return mappedTransactions;
        }

        private async Task<ExchangeRate> GetExchangeRateAsync(Guid officeId, Guid sourceCurrencyId, Guid targetCurrencyId)
        {
            return await _context.ExchangeRates
                .Where(r => r.OfficeId == officeId &&
                           r.SourceCurrencyId == sourceCurrencyId &&
                           r.TargetCurrencyId == targetCurrencyId &&
                           r.IsActive &&
                           r.EffectiveFrom <= DateTime.Now &&
                           (r.EffectiveTo == null || r.EffectiveTo > DateTime.Now))
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync();
        }

        private string GenerateTransactionNumber()
        {
            // Format: EX-YYYYMMDD-XXXXXX
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = new Random().Next(100000, 999999);
            return $"EX-{date}-{random}";
        }

        private Guid GetCurrentUserId()
        {

            return Guid.Parse(_validationService.GetUserID());
        }



        public async Task<vm_transaction> GetTransaction(Guid id)
        {
            var dbTransaction = await _context.Transactions
                .Include(x => x.Vault)
                .Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);

            // Get the deleted by user if exists
            string deletedByUsername = null;
            if (dbTransaction.deletedByUserId.HasValue)
            {
                var deletedByUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == dbTransaction.deletedByUserId.Value);
                deletedByUsername = deletedByUser?.Username;
            }

            vm_transaction vm_Transaction = new vm_transaction()
            {
                CreatedDate = dbTransaction.CreatedDate,
                Id = dbTransaction.Id,
                Notes = dbTransaction.Notes,
                Status = dbTransaction.Status,
                TransactionDate = dbTransaction.TransactionDate,
                TransactionNumber = dbTransaction.TransactionNumber,
                UserId = dbTransaction.UserId,
                User = _mapper.Map<vm_user>(dbTransaction.User),
                Type = dbTransaction.Type,
                VaultName = dbTransaction.Vault.Name,
                VaultId = dbTransaction.VaultId,
                CustomerId = dbTransaction.CustomerId,

                IsCustomRate = dbTransaction.IsCustomRate,
                Profit = dbTransaction.Profit,
                Details = new List<vm_transactiondetail>(),
                IsDeleted = dbTransaction.IsDeleted,
                DeletedReason = dbTransaction.DeletedReason,
                DeletedBy = deletedByUsername
            };

            var dbtDetails = _context.TransactionDetails
                .Include(x => x.Currency)
                .Where(x => x.TransactionId == id).ToList();

            foreach (var d in dbtDetails)
            {
                vm_transactiondetail vm_Transactiondetail = new vm_transactiondetail()
                {
                    Side = d.Side,
                    Status = vm_Transaction.Status,
                    Amount = d.Amount,
                    CurrencyName = d.Currency.CurrencyName,
                    CurrencySymbol = d.Currency.CurrencySymbol,
                    CurrencyCode = d.Currency.CurrencyCode,
                    Commission = d.Commission,
                    CurrencyId = d.CurrencyId,
                    NetAmount = d.NetAmount,
                    Rate = d.Rate,
                    TransactionDate = vm_Transaction.TransactionDate,
                    TransactionId = vm_Transaction.Id,
                    UserFirstName = vm_Transaction.User.Firstname,
                    UserLastName = vm_Transaction.User.Lastname,
                    Username = vm_Transaction.User.Username,
                    ActualBuyRate = d.ActualBuyRate,
                    ActualSellRate = d.ActualSellRate,
                    CustomRate = d.CustomRate,
                };
                vm_Transaction.Details.Add(vm_Transactiondetail);
            }


            return vm_Transaction;
        }

        public async Task RemoveTransaction(rm_removetransaction request)
        {
            var dbTransaction = _context.Transactions
                .Include(d => d.Details)
                .Include(t => t.Vault)
                .FirstOrDefault(x => x.Id == request.transactionId);

            if (dbTransaction == null)
                throw new ApiException(HttpStatusCode.NotFound, "Transaction couldn't be found");

            var currentUserId = Guid.Parse(_validationService.GetUserID());

            dbTransaction.IsDeleted = true;
            dbTransaction.DeletedReason = request.reason;
            dbTransaction.deletedByUserId = currentUserId;
            dbTransaction.Status = TransactionStatus.Cancelled;
            
            var user = _context.Users.FirstOrDefault(x => x.Id == currentUserId);

            var acLog = new ActionLog
            {
                Id = Guid.NewGuid(),
                ActionType = ActionType.Transaction,
                UserId = currentUserId,
                UserDescription = request.reason,
                LogType = LogType.Deleted,
                ContentId = dbTransaction.Id,
                Description = $"{user?.Username} {LogType.Deleted} {ActionType.Transaction} with this reason : {request.reason}",
                CreatedDate = DateTime.UtcNow,
                User = user
            };

            _context.Logs.Add(acLog);

            // Restore vault balances
            var dbHistories = _context.VaultBalanceHistories
                .Where(x => x.Description == "Exchange transaction " + dbTransaction.TransactionNumber)
                .ToList(); 

            foreach (var kvp in dbHistories)
            {
                kvp.IsDeleted = true;

                var dbVaultBalance = _context.VaultBalances
                    .FirstOrDefault(x => x.VaultId == kvp.VaultId && x.CurrencyId == kvp.CurrencyId);

                if (dbVaultBalance != null)
                {
                    // Reverse the balance change (if it was added, subtract; if subtracted, add back)
                    dbVaultBalance.Balance -= kvp.Balance;
                    dbVaultBalance.LastUpdated = DateTime.Now;
                }
            }
            
            // Restore party account balances if this was a party transaction
            if (dbTransaction.PartyId.HasValue)
            {
                await RestorePartyAccountBalances(dbTransaction);
            }
           
            await _context.SaveChangesAsync();
            
            // Clear vault caches to ensure fresh data
            _vaultService.ClearVaultCaches();
            
            // Invalidate Z-report cache AFTER saving changes
            if (dbTransaction.Vault != null)
            {
                InvalidateZReportCache(dbTransaction.Vault.OfficeId, dbTransaction.TransactionDate);
                InvalidateZReportCache(dbTransaction.Vault.OfficeId, DateTime.Today);
                // Also invalidate for DateTime.Now in case of timezone differences
                InvalidateZReportCache(dbTransaction.Vault.OfficeId, DateTime.Now.Date);
            }
        }
        
        private async Task RestorePartyAccountBalances(Transaction transaction)
        {
            // Get party account entries related to this transaction
            var partyEntries = await _context.PartyAccountEntries
                .Where(e => e.TransactionId == transaction.Id)
                .ToListAsync();
                
            foreach (var entry in partyEntries)
            {
                // Find the party account
                var partyAccount = await _context.PartyAccounts
                    .FirstOrDefaultAsync(pa => pa.Id == entry.PartyAccountId);
                    
                if (partyAccount != null)
                {
                    // Reverse the balance change
                    partyAccount.Balance -= entry.Amount;
                    
                    // Remove the entry from database
                    _context.PartyAccountEntries.Remove(entry);
                }
            }
        }

        
        private void InvalidateZReportCache(Guid officeId, DateTime date)
        {
            var cacheKey = $"ZReport_Daily_{officeId}_{date:yyyyMMdd}";
            _memoryCache.Remove(cacheKey);
        }
    }
}