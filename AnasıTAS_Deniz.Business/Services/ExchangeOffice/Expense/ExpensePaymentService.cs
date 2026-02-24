using AutoMapper;
using Microsoft.EntityFrameworkCore;
using AnasıTAS_Deniz.Business.Infrastructure.ExchangeOffice.Expense;
using AnasıTAS_Deniz.Business.Infrastructure.ExchangeOffice.Office;
using AnasıTAS_Deniz.Business.Services.Permission;
using AnasıTAS_Deniz.Data.Contexts;
using AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Expense;
using AnasıTAS_Deniz.Entity.Entities.ExchangeOffice.Office;
using AnasıTAS_Deniz.Entity.Modals.RequestModals.ExchangeService.Expense;
using AnasıTAS_Deniz.Entity.Modals.RequestModals.ExchangeService.Office;
using AnasıTAS_Deniz.Entity.Modals.ViewModals.ExchangeOFfice.Expense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Services.ExchangeOffice.Expense
{
    public class ExpensePaymentService : IExpensePaymentService
    {
        private readonly AnasıTAS_DenizDbContext _context;
        private readonly IMapper _mapper;
        private readonly IVaultService _vaultService;
        private readonly ValidationService _validationService;

        public ExpensePaymentService(
            AnasıTAS_DenizDbContext context, 
            IMapper mapper, 
            IVaultService vaultService,
            ValidationService validationService)
        {
            _context = context;
            _mapper = mapper;
            _vaultService = vaultService;
            _validationService = validationService;
        }

        public async Task<vm_expensepayment> CreatePaymentAsync(rm_expensepayment request)
        {
            // Get expense definition
            var definition = await _context.ExpenseDefinitions
                .Include(ed => ed.Office)
                .FirstOrDefaultAsync(ed => ed.Id == request.ExpenseDefinitionId);

            if (definition == null)
                throw new InvalidOperationException("Expense definition not found");

            if (!definition.IsActive)
                throw new InvalidOperationException("Expense definition is not active");

            // Get active vault for the office
            var vault = await _context.Vaults
                .FirstOrDefaultAsync(v => v.OfficeId == definition.OfficeId && v.IsActive);

            if (vault == null)
                throw new InvalidOperationException("No active vault found for this office");

            // Check vault balance
            var hasBalance = await _vaultService.CheckVaultBalanceAsync(vault.Id, request.CurrencyId, request.Amount);
            if (!hasBalance)
                throw new InvalidOperationException("Insufficient vault balance");

            // Get currency for description
            var currency = await _context.Currencies.FindAsync(request.CurrencyId);
            
            // Create payment
            var payment = new ExpensePayment
            {
                Id = Guid.NewGuid(),
                ExpenseDefinitionId = request.ExpenseDefinitionId,
                VaultId = vault.Id,
                CurrencyId = request.CurrencyId,
                PaymentNumber = GeneratePaymentNumber(),
                PaymentDate = request.PaymentDate,
                Amount = request.Amount,
                PaymentMethod = request.PaymentMethod,
                ReferenceNumber = request.ReferenceNumber,
                Description = request.Description,
                Receipt = request.Receipt,
                Status = ExpenseStatus.Paid,
                CreatedByUserId = Guid.Parse(_validationService.GetUserID()),
                CreatedDate = DateTime.UtcNow
            };

            _context.ExpensePayments.Add(payment);

            // Update vault balance - expense is always a withdrawal
            var vaultUpdate = new rm_updatevaultbalance
            {
                vaultId = vault.Id,
                currencyId = request.CurrencyId,
                amount = -request.Amount, // Negative because it's an expense
                description = GetTurkishExpenseDescription(definition, payment, currency?.CurrencyCode),
                isEntireBalance = false,
                TransactionType = TransactionType.Withdrawal
            };

            await _vaultService.UpdateVaultBalanceAsync(vaultUpdate);
            await _context.SaveChangesAsync();

            return await GetPaymentAsync(payment.Id);
        }

        public async Task<vm_expensepayment> GetPaymentAsync(Guid id)
        {
            var payment = await _context.ExpensePayments
                .Include(ep => ep.ExpenseDefinition)
                    .ThenInclude(ed => ed.Office)
                .Include(ep => ep.Vault)
                .Include(ep => ep.Currency)
                .Include(ep => ep.CreatedByUser)
                .Include(ep => ep.DeletedByUser)
                .FirstOrDefaultAsync(ep => ep.Id == id);

            if (payment == null)
                return null;

            return MapToViewModel(payment);
        }

        public async Task<List<vm_expensepayment>> GetPaymentsAsync(Guid officeId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.ExpensePayments
                .Include(ep => ep.ExpenseDefinition)
                    .ThenInclude(ed => ed.Office)
                .Include(ep => ep.Vault)
                .Include(ep => ep.Currency)
                .Include(ep => ep.CreatedByUser)
                .Include(ep => ep.DeletedByUser)
                .Where(ep => ep.ExpenseDefinition.OfficeId == officeId && !ep.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(ep => ep.PaymentDate >= startDate.Value.Date);

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(ep => ep.PaymentDate <= endOfDay);
            }

            var payments = await query
                .OrderByDescending(ep => ep.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToViewModel).ToList();
        }

        public async Task<List<vm_expensepayment>> GetPaymentsByDefinitionAsync(Guid definitionId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.ExpensePayments
                .Include(ep => ep.ExpenseDefinition)
                    .ThenInclude(ed => ed.Office)
                .Include(ep => ep.Vault)
                .Include(ep => ep.Currency)
                .Include(ep => ep.CreatedByUser)
                .Include(ep => ep.DeletedByUser)
                .Where(ep => ep.ExpenseDefinitionId == definitionId && !ep.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(ep => ep.PaymentDate >= startDate.Value.Date);

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(ep => ep.PaymentDate <= endOfDay);
            }

            var payments = await query
                .OrderByDescending(ep => ep.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToViewModel).ToList();
        }

        public async Task<bool> DeletePaymentAsync(Guid id, string reason)
        {
            var payment = await _context.ExpensePayments
                .Include(ep => ep.ExpenseDefinition)
                .Include(ep => ep.Currency)
                .FirstOrDefaultAsync(ep => ep.Id == id);

            if (payment == null || payment.IsDeleted)
                return false;

            // Mark as deleted
            payment.IsDeleted = true;
            payment.DeletedReason = reason;
            payment.DeletedByUserId = Guid.Parse(_validationService.GetUserID());
            payment.DeletedDate = DateTime.UtcNow;
            payment.Status = ExpenseStatus.Cancelled;

            // Reverse the vault transaction
            var vaultUpdate = new rm_updatevaultbalance
            {
                vaultId = payment.VaultId,
                currencyId = payment.CurrencyId,
                amount = payment.Amount, // Positive to reverse the expense
                description = $"Gider iptali: {payment.ExpenseDefinition.Name} - {reason}",
                isEntireBalance = false,
                TransactionType = TransactionType.Deposit
            };

            await _vaultService.UpdateVaultBalanceAsync(vaultUpdate);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<decimal> GetTotalExpensesAsync(Guid officeId, DateTime startDate, DateTime endDate)
        {
            // Get base currency
            var baseCurrency = await _context.Currencies.FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
            if (baseCurrency == null)
                return 0;

            var endOfDay = endDate.Date.AddDays(1).AddSeconds(-1);

            var expenses = await _context.ExpensePayments
                .Include(ep => ep.ExpenseDefinition)
                .Include(ep => ep.Currency)
                .Where(ep => ep.ExpenseDefinition.OfficeId == officeId &&
                           ep.PaymentDate >= startDate.Date &&
                           ep.PaymentDate <= endOfDay &&
                           !ep.IsDeleted &&
                           ep.Status == ExpenseStatus.Paid)
                .ToListAsync();

            decimal totalInTRY = 0;

            foreach (var expense in expenses)
            {
                if (expense.CurrencyId == baseCurrency.Id)
                {
                    totalInTRY += expense.Amount;
                }
                else
                {
                    // Get exchange rate to TRY
                    var rate = await GetExchangeRateToTRY(officeId, expense.CurrencyId);
                    totalInTRY += expense.Amount * rate;
                }
            }

            return totalInTRY;
        }

        public async Task<Dictionary<string, decimal>> GetExpensesByCategoryAsync(Guid officeId, DateTime startDate, DateTime endDate)
        {
            var baseCurrency = await _context.Currencies.FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
            if (baseCurrency == null)
                return new Dictionary<string, decimal>();

            var endOfDay = endDate.Date.AddDays(1).AddSeconds(-1);

            var expenses = await _context.ExpensePayments
                .Include(ep => ep.ExpenseDefinition)
                .Include(ep => ep.Currency)
                .Where(ep => ep.ExpenseDefinition.OfficeId == officeId &&
                           ep.PaymentDate >= startDate.Date &&
                           ep.PaymentDate <= endOfDay &&
                           !ep.IsDeleted &&
                           ep.Status == ExpenseStatus.Paid)
                .ToListAsync();

            var result = new Dictionary<string, decimal>();

            foreach (var expense in expenses)
            {
                var categoryName = GetCategoryName(expense.ExpenseDefinition.Category);
                
                decimal amountInTRY;
                if (expense.CurrencyId == baseCurrency.Id)
                {
                    amountInTRY = expense.Amount;
                }
                else
                {
                    var rate = await GetExchangeRateToTRY(officeId, expense.CurrencyId);
                    amountInTRY = expense.Amount * rate;
                }

                if (result.ContainsKey(categoryName))
                    result[categoryName] += amountInTRY;
                else
                    result[categoryName] = amountInTRY;
            }

            return result;
        }

        private async Task<decimal> GetExchangeRateToTRY(Guid officeId, Guid currencyId)
        {
            var baseCurrency = await _context.Currencies
                .FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");

            if (baseCurrency == null || currencyId == baseCurrency.Id)
                return 1m;

            var rate = await _context.ExchangeRates
                .Where(r => r.OfficeId == officeId &&
                           r.SourceCurrencyId == currencyId &&
                           r.TargetCurrencyId == baseCurrency.Id &&
                           r.IsActive)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync();

            return rate?.SellRate ?? 0m;
        }

        private string GeneratePaymentNumber()
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = new Random().Next(10000, 99999);
            return $"EXP-{date}-{random}";
        }

        private string GetTurkishExpenseDescription(ExpenseDefinition definition, ExpensePayment payment, string currencyCode)
        {
            var categoryName = GetCategoryName(definition.Category);
            var paymentMethod = GetTurkishPaymentMethod(payment.PaymentMethod);
            
            // Example: "Ahmet'e 30000 TRY maaş ödemesi yapıldı"
            if (definition.Category == ExpenseCategory.Salary)
            {
                return $"{definition.Name}'e {payment.Amount:N2} {currencyCode} maaş ödemesi yapıldı";
            }
            
            // Generic format: "Elektrik faturası için 5000 TRY ödeme yapıldı"
            return $"{definition.Name} için {payment.Amount:N2} {currencyCode} ödeme yapıldı ({paymentMethod})";
        }

        private string GetTurkishPaymentMethod(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.Cash => "Nakit",
                PaymentMethod.BankTransfer => "Banka Havalesi",
                PaymentMethod.CreditCard => "Kredi Kartı",
                PaymentMethod.Check => "Çek",
                PaymentMethod.Other => "Diğer",
                _ => method.ToString()
            };
        }

        private string GetCategoryName(ExpenseCategory category)
        {
            return category switch
            {
                ExpenseCategory.Salary => "Maaş",
                ExpenseCategory.Rent => "Kira",
                ExpenseCategory.Utilities => "Faturalar",
                ExpenseCategory.Office => "Ofis Giderleri",
                ExpenseCategory.Marketing => "Pazarlama",
                ExpenseCategory.Travel => "Seyahat",
                ExpenseCategory.Insurance => "Sigorta",
                ExpenseCategory.Tax => "Vergi",
                ExpenseCategory.Maintenance => "Bakım",
                ExpenseCategory.Other => "Diğer",
                _ => category.ToString()
            };
        }

        private vm_expensepayment MapToViewModel(ExpensePayment payment)
        {
            return new vm_expensepayment
            {
                Id = payment.Id,
                ExpenseDefinitionId = payment.ExpenseDefinitionId,
                ExpenseDefinitionName = payment.ExpenseDefinition?.Name,
                ExpenseDefinitionCode = payment.ExpenseDefinition?.Code,
                Category = payment.ExpenseDefinition?.Category ?? ExpenseCategory.Other,
                CategoryName = GetCategoryName(payment.ExpenseDefinition?.Category ?? ExpenseCategory.Other),
                VaultId = payment.VaultId,
                VaultName = payment.Vault?.Name,
                CurrencyId = payment.CurrencyId,
                CurrencyCode = payment.Currency?.CurrencyCode,
                CurrencySymbol = payment.Currency?.CurrencySymbol,
                PaymentNumber = payment.PaymentNumber,
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                PaymentMethodName = GetTurkishPaymentMethod(payment.PaymentMethod),
                ReferenceNumber = payment.ReferenceNumber,
                Description = payment.Description,
                Receipt = payment.Receipt,
                Status = payment.Status,
                StatusName = GetStatusName(payment.Status),
                IsDeleted = payment.IsDeleted,
                DeletedReason = payment.DeletedReason,
                DeletedDate = payment.DeletedDate,
                DeletedByUserName = payment.DeletedByUser?.Username,
                CreatedDate = payment.CreatedDate,
                CreatedByUserName = payment.CreatedByUser?.Username
            };
        }

        private string GetStatusName(ExpenseStatus status)
        {
            return status switch
            {
                ExpenseStatus.Pending => "Beklemede",
                ExpenseStatus.Paid => "Ödendi",
                ExpenseStatus.Cancelled => "İptal Edildi",
                ExpenseStatus.Refunded => "İade Edildi",
                _ => status.ToString()
            };
        }
    }
}