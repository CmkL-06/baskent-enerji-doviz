using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoneyTransferTurkey.Business.Infrastructure.ExchangeOffice.Party;
using MoneyTransferTurkey.Data.Contexts;
using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Office;
using MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Party;
using MoneyTransferTurkey.Entity;
using MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Party;
using MoneyTransferTurkey.Entity.Modals.ViewModals.ExchangeOffice.Party;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Services.ExchangeOffice.Party
{
    public class GhostPartyService : IGhostPartyService
    {
        private readonly MoneyTransferTurkeyDbContext _context;
        private readonly IMapper _mapper;

        public GhostPartyService(MoneyTransferTurkeyDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // Account Management
        public async Task<vm_ghostpartyaccount> CreateGhostAccountAsync(rm_ghostpartyaccount request)
        {
            var existingAccount = await _context.Set<GhostPartyAccount>()
                .FirstOrDefaultAsync(x => x.PartyId == request.PartyId && 
                                        x.CurrencyId == request.CurrencyId && 
                                        x.OfficeId == request.OfficeId);

            if (existingAccount != null)
            {
                return _mapper.Map<vm_ghostpartyaccount>(existingAccount);
            }

            var account = new GhostPartyAccount
            {
                Id = Guid.NewGuid(),
                PartyId = request.PartyId,
                OfficeId = request.OfficeId,
                CurrencyId = request.CurrencyId,
                Balance = request.InitialBalance,
                Note = request.Note,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.Set<GhostPartyAccount>().Add(account);
            await _context.SaveChangesAsync();

            return await GetGhostAccountAsync(account.Id);
        }

        public async Task<vm_ghostpartyaccount> GetGhostAccountAsync(Guid accountId)
        {
            var account = await _context.Set<GhostPartyAccount>()
                .Include(x => x.Party)
                .Include(x => x.Office)
                .Include(x => x.Currency)
                .FirstOrDefaultAsync(x => x.Id == accountId);

            if (account == null)
                return null;

            return new vm_ghostpartyaccount
            {
                Id = account.Id,
                PartyId = account.PartyId,
                PartyName = account.Party?.Name,
                PartyCode = account.Party?.PartyCode,
                OfficeId = account.OfficeId,
                OfficeName = account.Office?.OfficeName,
                CurrencyId = account.CurrencyId,
                CurrencyCode = account.Currency?.CurrencyCode,
                CurrencyName = account.Currency?.CurrencyName,
                Balance = account.Balance,
                BlockedAmount = account.BlockedAmount,
                AvailableBalance = account.AvailableBalance,
                TotalDebits = account.TotalDebits,
                TotalCredits = account.TotalCredits,
                TransactionCount = account.TransactionCount,
                LastTransactionDate = account.LastTransactionDate,
                Note = account.Note,
                IsActive = account.IsActive,
                CreatedDate = account.CreatedDate,
                ModifiedDate = account.UpdatedDate
            };
        }

        public async Task<List<vm_ghostpartyaccount>> GetGhostAccountsByPartyAsync(Guid partyId)
        {
            var accounts = await _context.Set<GhostPartyAccount>()
                .Include(x => x.Party)
                .Include(x => x.Office)
                .Include(x => x.Currency)
                .Where(x => x.PartyId == partyId && x.IsActive)
                .ToListAsync();

            return accounts.Select(account => new vm_ghostpartyaccount
            {
                Id = account.Id,
                PartyId = account.PartyId,
                PartyName = account.Party?.Name,
                PartyCode = account.Party?.PartyCode,
                OfficeId = account.OfficeId,
                OfficeName = account.Office?.OfficeName,
                CurrencyId = account.CurrencyId,
                CurrencyCode = account.Currency?.CurrencyCode,
                CurrencyName = account.Currency?.CurrencyName,
                Balance = account.Balance,
                BlockedAmount = account.BlockedAmount,
                AvailableBalance = account.AvailableBalance,
                TotalDebits = account.TotalDebits,
                TotalCredits = account.TotalCredits,
                TransactionCount = account.TransactionCount,
                LastTransactionDate = account.LastTransactionDate,
                Note = account.Note,
                IsActive = account.IsActive,
                CreatedDate = account.CreatedDate,
                ModifiedDate = account.UpdatedDate
            }).ToList();
        }

        public async Task<vm_ghostpartyaccount> GetGhostAccountByCurrencyAsync(Guid partyId, Guid currencyId)
        {
            var account = await _context.Set<GhostPartyAccount>()
                .Include(x => x.Party)
                .Include(x => x.Office)
                .Include(x => x.Currency)
                .FirstOrDefaultAsync(x => x.PartyId == partyId && x.CurrencyId == currencyId && x.IsActive);

            if (account == null)
                return null;

            return new vm_ghostpartyaccount
            {
                Id = account.Id,
                PartyId = account.PartyId,
                PartyName = account.Party?.Name,
                PartyCode = account.Party?.PartyCode,
                OfficeId = account.OfficeId,
                OfficeName = account.Office?.OfficeName,
                CurrencyId = account.CurrencyId,
                CurrencyCode = account.Currency?.CurrencyCode,
                CurrencyName = account.Currency?.CurrencyName,
                Balance = account.Balance,
                BlockedAmount = account.BlockedAmount,
                AvailableBalance = account.AvailableBalance,
                TotalDebits = account.TotalDebits,
                TotalCredits = account.TotalCredits,
                TransactionCount = account.TransactionCount,
                LastTransactionDate = account.LastTransactionDate,
                Note = account.Note,
                IsActive = account.IsActive,
                CreatedDate = account.CreatedDate,
                ModifiedDate = account.UpdatedDate
            };
        }

        public async Task<vm_ghostpartyaccount> BlockAmountAsync(Guid accountId, decimal amount, string reason)
        {
            var account = await _context.Set<GhostPartyAccount>().FindAsync(accountId);
            if (account == null)
                throw new Exception("Ghost account not found");

            if (account.AvailableBalance < amount)
                throw new Exception("Insufficient available balance");

            account.BlockedAmount += amount;
            account.UpdatedDate = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return await GetGhostAccountAsync(accountId);
        }

        public async Task<vm_ghostpartyaccount> UnblockAmountAsync(Guid accountId, decimal amount)
        {
            var account = await _context.Set<GhostPartyAccount>().FindAsync(accountId);
            if (account == null)
                throw new Exception("Ghost account not found");

            if (account.BlockedAmount < amount)
                throw new Exception("Blocked amount is less than the amount to unblock");

            account.BlockedAmount -= amount;
            account.UpdatedDate = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return await GetGhostAccountAsync(accountId);
        }

        public async Task<decimal> GetAvailableBalanceAsync(Guid accountId)
        {
            var account = await _context.Set<GhostPartyAccount>().FindAsync(accountId);
            return account?.AvailableBalance ?? 0;
        }

        // Entry Management
        public async Task<vm_ghostpartyentry> CreateGhostEntryAsync(rm_ghostpartyentry request)
        {
            var account = await _context.Set<GhostPartyAccount>()
                .Include(x => x.Party)
                .FirstOrDefaultAsync(x => x.Id == request.GhostAccountId);
            
            if (account == null)
                throw new Exception("Ghost account not found");

            var entry = new GhostPartyAccountEntry
            {
                Id = Guid.NewGuid(),
                GhostAccountId = request.GhostAccountId,
                OfficeId = request.OfficeId,
                CurrencyId = request.CurrencyId,
                VaultId = request.VaultId,
                EntryType = request.EntryType,
                Amount = request.Amount,
                ReferenceNumber = request.ReferenceNumber,
                Description = request.Description,
                PaymentMethod = request.PaymentMethod,
                Note = request.Note,
                Status = "Completed",
                TransactionDate = DateTime.UtcNow,
                CreatedBy = request.CreatedBy,
                CreatedDate = DateTime.UtcNow
            };

            // Update account balance and running balance
            if (request.EntryType == "Debit")
            {
                account.Balance += request.Amount;
                account.TotalDebits += request.Amount;
            }
            else if (request.EntryType == "Credit")
            {
                account.Balance -= request.Amount;
                account.TotalCredits += request.Amount;
            }

            entry.RunningBalance = account.Balance;
            account.TransactionCount++;
            account.LastTransactionDate = DateTime.UtcNow;
            account.UpdatedDate = DateTime.UtcNow;

            _context.Set<GhostPartyAccountEntry>().Add(entry);

            // Update vault balance
            await UpdateVaultForGhostEntryAsync(request.VaultId, request.CurrencyId, 
                request.Amount, request.EntryType == "Debit", request.CreatedBy);

            await _context.SaveChangesAsync();
            return await GetGhostEntryAsync(entry.Id);
        }

        public async Task<List<vm_ghostpartyentry>> GetGhostEntriesAsync(Guid accountId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Set<GhostPartyAccountEntry>()
                .Include(x => x.GhostAccount)
                    .ThenInclude(x => x.Party)
                .Include(x => x.Office)
                .Include(x => x.Currency)
                .Include(x => x.Vault)
                .Where(x => x.GhostAccountId == accountId);

            if (fromDate.HasValue)
                query = query.Where(x => x.TransactionDate >= fromDate.Value);
            
            if (toDate.HasValue)
                query = query.Where(x => x.TransactionDate <= toDate.Value);

            var entries = await query.OrderByDescending(x => x.TransactionDate).ToListAsync();

            return entries.Select(entry => new vm_ghostpartyentry
            {
                Id = entry.Id,
                GhostAccountId = entry.GhostAccountId,
                PartyId = entry.GhostAccount?.PartyId ?? Guid.Empty,
                PartyName = entry.GhostAccount?.Party?.Name,
                OfficeId = entry.OfficeId,
                OfficeName = entry.Office?.OfficeName,
                CurrencyId = entry.CurrencyId,
                CurrencyCode = entry.Currency?.CurrencyCode,
                EntryType = entry.EntryType,
                Amount = entry.Amount,
                RunningBalance = entry.RunningBalance,
                ReferenceNumber = entry.ReferenceNumber,
                Description = entry.Description,
                TransactionDate = entry.TransactionDate,
                TransactionId = entry.TransactionId,
                VaultId = entry.VaultId,
                VaultName = entry.Vault?.Name,
                PaymentMethod = entry.PaymentMethod,
                Status = entry.Status,
                IsReconciled = entry.IsReconciled,
                ReconciledDate = entry.ReconciledDate,
                ReconciledBy = entry.ReconciledBy,
                Note = entry.Note,
                CreatedBy = entry.CreatedBy,
                CreatedDate = entry.CreatedDate,
                ModifiedBy = entry.UpdatedBy,
                ModifiedDate = entry.UpdatedDate
            }).ToList();
        }

        public async Task<vm_ghostpartyentry> GetGhostEntryAsync(Guid entryId)
        {
            var entry = await _context.Set<GhostPartyAccountEntry>()
                .Include(x => x.GhostAccount)
                    .ThenInclude(x => x.Party)
                .Include(x => x.Office)
                .Include(x => x.Currency)
                .Include(x => x.Vault)
                .FirstOrDefaultAsync(x => x.Id == entryId);

            if (entry == null)
                return null;

            return new vm_ghostpartyentry
            {
                Id = entry.Id,
                GhostAccountId = entry.GhostAccountId,
                PartyId = entry.GhostAccount?.PartyId ?? Guid.Empty,
                PartyName = entry.GhostAccount?.Party?.Name,
                OfficeId = entry.OfficeId,
                OfficeName = entry.Office?.OfficeName,
                CurrencyId = entry.CurrencyId,
                CurrencyCode = entry.Currency?.CurrencyCode,
                EntryType = entry.EntryType,
                Amount = entry.Amount,
                RunningBalance = entry.RunningBalance,
                ReferenceNumber = entry.ReferenceNumber,
                Description = entry.Description,
                TransactionDate = entry.TransactionDate,
                TransactionId = entry.TransactionId,
                VaultId = entry.VaultId,
                VaultName = entry.Vault?.Name,
                PaymentMethod = entry.PaymentMethod,
                Status = entry.Status,
                IsReconciled = entry.IsReconciled,
                ReconciledDate = entry.ReconciledDate,
                ReconciledBy = entry.ReconciledBy,
                Note = entry.Note,
                CreatedBy = entry.CreatedBy,
                CreatedDate = entry.CreatedDate,
                ModifiedBy = entry.UpdatedBy,
                ModifiedDate = entry.UpdatedDate
            };
        }

        public async Task<vm_ghostpartyentry> ReverseGhostEntryAsync(Guid entryId, string reason)
        {
            var originalEntry = await _context.Set<GhostPartyAccountEntry>()
                .Include(x => x.GhostAccount)
                .FirstOrDefaultAsync(x => x.Id == entryId);

            if (originalEntry == null)
                throw new Exception("Ghost entry not found");

            if (originalEntry.Status == "Reversed")
                throw new Exception("Entry is already reversed");

            var reverseEntry = new GhostPartyAccountEntry
            {
                Id = Guid.NewGuid(),
                GhostAccountId = originalEntry.GhostAccountId,
                OfficeId = originalEntry.OfficeId,
                CurrencyId = originalEntry.CurrencyId,
                VaultId = originalEntry.VaultId,
                EntryType = originalEntry.EntryType == "Debit" ? "Credit" : "Debit",
                Amount = originalEntry.Amount,
                ReferenceNumber = $"REV-{originalEntry.ReferenceNumber}",
                Description = $"Reversal: {reason}",
                PaymentMethod = originalEntry.PaymentMethod,
                Note = $"Reversal of entry {originalEntry.Id}: {reason}",
                Status = "Completed",
                TransactionDate = DateTime.UtcNow,
                CreatedBy = originalEntry.CreatedBy,
                CreatedDate = DateTime.UtcNow
            };

            // Update account balance
            var account = originalEntry.GhostAccount;
            if (reverseEntry.EntryType == "Debit")
            {
                account.Balance += reverseEntry.Amount;
                account.TotalDebits += reverseEntry.Amount;
            }
            else
            {
                account.Balance -= reverseEntry.Amount;
                account.TotalCredits += reverseEntry.Amount;
            }

            reverseEntry.RunningBalance = account.Balance;
            account.TransactionCount++;
            account.LastTransactionDate = DateTime.UtcNow;
            account.UpdatedDate = DateTime.UtcNow;

            originalEntry.Status = "Reversed";
            originalEntry.UpdatedDate = DateTime.UtcNow;

            _context.Set<GhostPartyAccountEntry>().Add(reverseEntry);

            // Update vault balance
            await UpdateVaultForGhostEntryAsync(reverseEntry.VaultId.Value, reverseEntry.CurrencyId,
                reverseEntry.Amount, reverseEntry.EntryType == "Debit", reverseEntry.CreatedBy);

            await _context.SaveChangesAsync();
            return await GetGhostEntryAsync(reverseEntry.Id);
        }

        public async Task<bool> ReconcileGhostEntryAsync(Guid entryId, Guid userId)
        {
            var entry = await _context.Set<GhostPartyAccountEntry>().FindAsync(entryId);
            if (entry == null)
                return false;

            entry.IsReconciled = true;
            entry.ReconciledDate = DateTime.UtcNow;
            entry.ReconciledBy = userId;
            entry.UpdatedDate = DateTime.UtcNow;
            entry.UpdatedBy = userId;

            await _context.SaveChangesAsync();
            return true;
        }

        // Transaction Processing
        public async Task<vm_ghostpartyentry> ProcessGhostPaymentAsync(rm_ghostpartypayment request)
        {
            // Get or create ghost account
            var account = await _context.Set<GhostPartyAccount>()
                .FirstOrDefaultAsync(x => x.PartyId == request.PartyId && 
                                        x.CurrencyId == request.CurrencyId && 
                                        x.OfficeId == request.OfficeId);

            if (account == null)
            {
                var createAccountRequest = new rm_ghostpartyaccount
                {
                    PartyId = request.PartyId,
                    OfficeId = request.OfficeId,
                    CurrencyId = request.CurrencyId,
                    InitialBalance = 0,
                    Note = "Auto-created for ghost payment"
                };
                var accountResult = await CreateGhostAccountAsync(createAccountRequest);
                account = await _context.Set<GhostPartyAccount>().FindAsync(accountResult.Id);
            }

            // Create debit entry (payment from party to us - increases vault balance)
            var entryRequest = new rm_ghostpartyentry
            {
                GhostAccountId = account.Id,
                OfficeId = request.OfficeId,
                CurrencyId = request.CurrencyId,
                VaultId = request.VaultId,
                EntryType = "Credit",  // Credit to party account (they paid us)
                Amount = request.Amount,
                ReferenceNumber = request.ReferenceNumber,
                Description = request.Description,
                PaymentMethod = request.PaymentMethod,
                Note = request.Note,
                CreatedBy = request.CreatedBy
            };

            return await CreateGhostEntryAsync(entryRequest);
        }

        public async Task<vm_ghostpartyentry> ProcessGhostCollectionAsync(rm_ghostpartycollection request)
        {
            // Get or create ghost account
            var account = await _context.Set<GhostPartyAccount>()
                .FirstOrDefaultAsync(x => x.PartyId == request.PartyId && 
                                        x.CurrencyId == request.CurrencyId && 
                                        x.OfficeId == request.OfficeId);

            if (account == null)
            {
                var createAccountRequest = new rm_ghostpartyaccount
                {
                    PartyId = request.PartyId,
                    OfficeId = request.OfficeId,
                    CurrencyId = request.CurrencyId,
                    InitialBalance = 0,
                    Note = "Auto-created for ghost collection"
                };
                var accountResult = await CreateGhostAccountAsync(createAccountRequest);
                account = await _context.Set<GhostPartyAccount>().FindAsync(accountResult.Id);
            }

            // Create credit entry (collection to party - decreases vault balance)
            var entryRequest = new rm_ghostpartyentry
            {
                GhostAccountId = account.Id,
                OfficeId = request.OfficeId,
                CurrencyId = request.CurrencyId,
                VaultId = request.VaultId,
                EntryType = "Debit",  // Debit to party account (we paid them)
                Amount = request.Amount,
                ReferenceNumber = request.ReferenceNumber,
                Description = request.Description,
                PaymentMethod = request.PaymentMethod,
                Note = request.Note,
                CreatedBy = request.CreatedBy
            };

            return await CreateGhostEntryAsync(entryRequest);
        }

        // Balance and Reporting
        public async Task<vm_ghostpartybalance> GetGhostBalanceSummaryAsync(Guid partyId)
        {
            var accounts = await _context.Set<GhostPartyAccount>()
                .Include(x => x.Party)
                .Include(x => x.Currency)
                .Where(x => x.PartyId == partyId && x.IsActive)
                .ToListAsync();

            var party = await _context.Set<Entity.Entities.ExchangeOffice.Party.Party>()
                .FirstOrDefaultAsync(x => x.Id == partyId);

            var balances = accounts.Select(acc => new GhostCurrencyBalance
            {
                CurrencyId = acc.CurrencyId,
                CurrencyCode = acc.Currency?.CurrencyCode,
                CurrencyName = acc.Currency?.CurrencyName,
                Balance = acc.Balance,
                BlockedAmount = acc.BlockedAmount,
                AvailableBalance = acc.AvailableBalance,
                TotalDebits = acc.TotalDebits,
                TotalCredits = acc.TotalCredits,
                TransactionCount = acc.TransactionCount,
                LastTransactionDate = acc.LastTransactionDate
            }).ToList();

            return new vm_ghostpartybalance
            {
                PartyId = partyId,
                PartyName = party?.Name,
                PartyCode = party?.PartyCode,
                CurrencyBalances = balances,
                TotalBalanceInBaseCurrency = balances.Sum(x => x.Balance), // Note: Should apply exchange rates
                TotalBlockedInBaseCurrency = balances.Sum(x => x.BlockedAmount),
                TotalAvailableInBaseCurrency = balances.Sum(x => x.AvailableBalance),
                LastActivityDate = accounts.Max(x => x.LastTransactionDate)
            };
        }

        public async Task<List<vm_ghostpartystatement>> GetGhostStatementAsync(Guid partyId, DateTime fromDate, DateTime toDate)
        {
            var entries = await _context.Set<GhostPartyAccountEntry>()
                .Include(x => x.GhostAccount)
                .Include(x => x.Currency)
                .Where(x => x.GhostAccount.PartyId == partyId &&
                          x.TransactionDate >= fromDate &&
                          x.TransactionDate <= toDate)
                .OrderBy(x => x.TransactionDate)
                .ToListAsync();

            return entries.Select(entry => new vm_ghostpartystatement
            {
                Date = entry.TransactionDate,
                ReferenceNumber = entry.ReferenceNumber,
                Description = entry.Description,
                EntryType = entry.EntryType,
                Debit = entry.EntryType == "Debit" ? entry.Amount : (decimal?)null,
                Credit = entry.EntryType == "Credit" ? entry.Amount : (decimal?)null,
                RunningBalance = entry.RunningBalance,
                CurrencyCode = entry.Currency?.CurrencyCode,
                PaymentMethod = entry.PaymentMethod,
                Status = entry.Status,
                Note = entry.Note
            }).ToList();
        }

        public async Task<vm_ghostpartysummary> GetGhostPartySummaryAsync(Guid partyId)
        {
            var party = await _context.Set<Entity.Entities.ExchangeOffice.Party.Party>()
                .FirstOrDefaultAsync(x => x.Id == partyId);

            var accounts = await GetGhostAccountsByPartyAsync(partyId);

            var entries = await _context.Set<GhostPartyAccountEntry>()
                .Include(x => x.GhostAccount)
                .Include(x => x.Currency)
                .Where(x => x.GhostAccount.PartyId == partyId)
                .ToListAsync();

            var balancesByCurrency = accounts
                .GroupBy(x => x.CurrencyCode)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Balance));

            var transactionCountByCurrency = accounts
                .GroupBy(x => x.CurrencyCode)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.TransactionCount));

            return new vm_ghostpartysummary
            {
                PartyId = partyId,
                PartyName = party?.Name,
                PartyCode = party?.PartyCode,
                PartyType = party?.Type.ToString(),
                TotalAccounts = accounts.Count,
                ActiveAccounts = accounts.Count(x => x.IsActive),
                TotalVolumeInBaseCurrency = accounts.Sum(x => x.TotalDebits + x.TotalCredits),
                FirstTransactionDate = entries.Min(x => x.TransactionDate),
                LastTransactionDate = entries.Max(x => x.TransactionDate),
                Accounts = accounts,
                TotalBalancesByCurrency = balancesByCurrency,
                TransactionCountByCurrency = transactionCountByCurrency
            };
        }

        // Vault Integration
        public async Task UpdateVaultForGhostEntryAsync(Guid vaultId, Guid currencyId, decimal amount, bool isDebit, Guid userId)
        {
            // Get vault balance for the specific currency
            var vaultBalance = await _context.Set<VaultBalance>()
                .FirstOrDefaultAsync(x => x.VaultId == vaultId && x.CurrencyId == currencyId);

            if (vaultBalance == null)
            {
                // Create new vault balance if it doesn't exist
                vaultBalance = new VaultBalance
                {
                    Id = Guid.NewGuid(),
                    VaultId = vaultId,
                    CurrencyId = currencyId,
                    Balance = 0,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Set<VaultBalance>().Add(vaultBalance);
            }

            // For ghost entries: 
            // Debit (party owes us) = money comes IN to vault
            // Credit (we owe party) = money goes OUT from vault
            if (isDebit)
            {
                vaultBalance.Balance -= amount;  // Money goes out
            }
            else
            {
                vaultBalance.Balance += amount;  // Money comes in
            }

            // Create vault balance history with IsGhost flag
            var history = new VaultBalanceHistory
            {
                Id = Guid.NewGuid(),
                VaultId = vaultId,
                CurrencyId = currencyId,
                UserId = userId,
                Balance = amount,
                Description = isDebit ? "Ghost party collection" : "Ghost party payment",
                TransactionType = isDebit ? TransactionType.Withdrawal : TransactionType.Deposit,
                IsGhost = true,  // Mark as ghost transaction
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow,
                IsParty = true
            };

            _context.Set<VaultBalanceHistory>().Add(history);
        }

        public async Task<bool> ValidateGhostTransactionAsync(Guid partyId, Guid currencyId, decimal amount, bool isDebit)
        {
            var account = await _context.Set<GhostPartyAccount>()
                .FirstOrDefaultAsync(x => x.PartyId == partyId && x.CurrencyId == currencyId && x.IsActive);

            if (account == null)
                return true; // New account will be created

            if (isDebit)
            {
                // Check if we have enough balance to pay
                return account.Balance >= amount;
            }

            return true; // Credits are always allowed
        }
    }
}