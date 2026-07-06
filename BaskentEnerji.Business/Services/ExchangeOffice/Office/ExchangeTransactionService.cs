using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.Client;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.ExchangeOffice.Party;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using BaskentEnerji.Entity.Modals.RequestModals.Site.Reports;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Office;
using BaskentEnerji.Entity.Modals.ViewModals.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Office
{
    public class ExchangeTransactionService : IExchangeTransactionService
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly IMapper _mapper;
        private readonly IVaultService _vaultService;
        private readonly IWacService _wacService;
        private readonly ValidationService _validationService;
        private readonly PartyTransactionIntegration _partyIntegration;
        private readonly IMemoryCache _memoryCache;
        private readonly IDayClosureService _dayClosureService;

        public ExchangeTransactionService(
            BaskentEnerjiDbContext context,
            IMapper mapper,
            IVaultService vaultService,
            IWacService wacService,
            ValidationService validationService,
            PartyTransactionIntegration partyIntegration, IMemoryCache memoryCache,
            IDayClosureService dayClosureService
            )
        {
            _context = context;
            _mapper = mapper;
            _vaultService = vaultService;
            _wacService = wacService;
            _validationService = validationService;
            _partyIntegration = partyIntegration;
            _memoryCache = memoryCache;
            _dayClosureService = dayClosureService;
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

                // Check day closure status — block transactions if unclosed days exist
                var canTransact = await _dayClosureService.CanTransactAsync(vault.OfficeId);
                if (!canTransact)
                    throw new ApiException(HttpStatusCode.BadRequest, "Önceki günün kapanışı yapılmadan işlem yapılamaz.");

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
                    Type = request.First().IsBuyingFromCustomer ? TransactionType.Buy : TransactionType.Exchange,
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

                // Pre-fetch all needed currencies to avoid N+1 queries inside loop
                var allCurrencyIds = request.SelectMany(r => new[] { r.SourceCurrencyId, r.TargetCurrencyId }).Distinct().ToList();
                var currencyDict = await _context.Currencies.Where(c => allCurrencyIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c);

                // Save transaction to DB first so WAC history FK references are valid
                _context.Transactions.Add(exchangeTransaction);
                await _context.SaveChangesAsync();

                // Collect details separately to avoid EF tracking issues after initial save
                var pendingDetails = new List<TransactionDetail>();

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
                    var marketRate = singleRequest.IsBuyingFromCustomer ? exchangeRate.BuyRate : exchangeRate.SellRate;
                    var rate = singleRequest.CustomRate ?? marketRate;

                    // Validate custom rate is within acceptable bounds (max 20% deviation from market)
                    if (singleRequest.CustomRate.HasValue && marketRate > 0)
                    {
                        var deviation = Math.Abs(rate - marketRate) / marketRate;
                        if (deviation > 0.20m && !singleRequest.OwnerOverrideLoss)
                            throw new ApiException(HttpStatusCode.BadRequest, $"Özel kur piyasa kurundan %{deviation * 100:F1} sapıyor. Onay için Owner yetkisi gereklidir.");
                    }

                    var targetAmount = singleRequest.SourceAmount * rate;
                    var netTargetAmount = targetAmount; // No commission deduction

                    // Calculate profit using WAC (Weighted Average Cost)
                    decimal profit = 0;

                    // Determine which currency is the foreign one (non-TRY)
                    var sourceCurrencyEntity = currencyDict.GetValueOrDefault(singleRequest.SourceCurrencyId);
                    var targetCurrencyEntity = currencyDict.GetValueOrDefault(singleRequest.TargetCurrencyId);
                    var isTrySource = sourceCurrencyEntity?.CurrencyCode == "TRY";
                    var isTryTarget = targetCurrencyEntity?.CurrencyCode == "TRY";

                    if (singleRequest.IsBuyingFromCustomer)
                    {
                        // Office buys foreign currency from customer → update WAC, no realized profit
                        if (!isTrySource)
                        {
                            await _wacService.RecalculateWacOnPurchaseAsync(vaultId, singleRequest.SourceCurrencyId, singleRequest.SourceAmount, rate, exchangeTransaction.Id);
                        }
                        profit = 0;
                    }
                    else
                    {
                        // Office sells foreign currency to customer → realized profit = (SellRate - WAC) * Qty
                        if (!isTrySource)
                        {
                            // Verify sufficient balance before selling
                            var currentBalance = await _context.VaultBalances
                                .Where(vb => vb.VaultId == vaultId && vb.CurrencyId == singleRequest.SourceCurrencyId)
                                .Select(vb => vb.Balance)
                                .FirstOrDefaultAsync();
                            if (currentBalance < singleRequest.SourceAmount)
                                throw new ApiException(HttpStatusCode.BadRequest, $"Insufficient vault balance");

                            profit = await _wacService.CalculateRealizedProfitAsync(rate, singleRequest.SourceAmount, vaultId, singleRequest.SourceCurrencyId);
                            var newQty = currentBalance - singleRequest.SourceAmount;
                            await _wacService.AdjustWacQuantityAsync(vaultId, singleRequest.SourceCurrencyId, newQty, Infrastructure.ExchangeOffice.Office.WacAdjustReason.Sale, exchangeTransaction.Id);
                        }
                        else
                        {
                            profit = (rate - exchangeRate.BuyRate) * singleRequest.SourceAmount;
                        }
                    }

                    totalProfit += profit;

                    // Create transaction details for this exchange
                    if (singleRequest.IsBuyingFromCustomer)
                    {
                        // Customer sells to us: we debit target currency, credit source currency
                        pendingDetails.Add(new TransactionDetail
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

                        pendingDetails.Add(new TransactionDetail
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
                        pendingDetails.Add(new TransactionDetail
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

                        pendingDetails.Add(new TransactionDetail
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

                // Add details via context to ensure they are tracked as "Added"
                foreach (var d in pendingDetails)
                {
                    d.TransactionId = exchangeTransaction.Id;
                    _context.Set<TransactionDetail>().Add(d);
                }

                // Save updated profit and details
                await _context.SaveChangesAsync();

                // Update vault balances
                foreach (var detail in pendingDetails)
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
                        TransactionType = exchangeTransaction.Type
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
                    (t.Type == TransactionType.Exchange || t.Type == TransactionType.Buy));

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

            // PERFORMANCE FIX: Create dictionary for O(1) lookup instead of O(nÂ²)
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
            var random = RandomNumberGenerator.GetInt32(100000, 1000000);
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
            using var dbTx = await _context.Database.BeginTransactionAsync();
            try
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
                        dbVaultBalance.Balance -= kvp.Balance;
                        dbVaultBalance.LastUpdated = DateTime.Now;
                    }
                }

                // Reverse WAC changes for exchange transactions
                if ((dbTransaction.Type == TransactionType.Exchange || dbTransaction.Type == TransactionType.Buy) && dbTransaction.Details != null)
                {
                    var detailCurrencyIds = dbTransaction.Details.Select(d => d.CurrencyId).Distinct().ToList();
                    var detailCurrencies = await _context.Currencies.Where(c => detailCurrencyIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c);

                    foreach (var detail in dbTransaction.Details)
                    {
                        detailCurrencies.TryGetValue(detail.CurrencyId, out var currency);
                        if (currency == null || currency.CurrencyCode == "TRY") continue;

                        if (detail.Side == TransactionSide.Credit && detail.Amount > 0)
                        {
                            await _wacService.ReverseWacOnPurchaseDeleteAsync(
                                dbTransaction.VaultId, detail.CurrencyId,
                                Math.Abs(detail.Amount), detail.Rate, dbTransaction.Id);
                        }
                        else if (detail.Side == TransactionSide.Debit && detail.Amount > 0)
                        {
                            var currentBalance = await _context.VaultBalances
                                .Where(vb => vb.VaultId == dbTransaction.VaultId && vb.CurrencyId == detail.CurrencyId)
                                .Select(vb => vb.Balance)
                                .FirstOrDefaultAsync();
                            await _wacService.AdjustWacQuantityAsync(
                                dbTransaction.VaultId, detail.CurrencyId,
                                currentBalance, WacAdjustReason.TransactionDelete, dbTransaction.Id);
                        }
                    }
                }

                // Restore party account balances if this was a party transaction
                if (dbTransaction.PartyId.HasValue)
                {
                    await RestorePartyAccountBalances(dbTransaction);
                }

                await _context.SaveChangesAsync();
                await dbTx.CommitAsync();

                // Clear vault caches to ensure fresh data
                _vaultService.ClearVaultCaches();

                // Invalidate Z-report cache AFTER commit
                if (dbTransaction.Vault != null)
                {
                    InvalidateZReportCache(dbTransaction.Vault.OfficeId, dbTransaction.TransactionDate);
                    InvalidateZReportCache(dbTransaction.Vault.OfficeId, DateTime.Today);
                    InvalidateZReportCache(dbTransaction.Vault.OfficeId, DateTime.Now.Date);
                }
            }
            catch
            {
                await dbTx.RollbackAsync();
                throw;
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
