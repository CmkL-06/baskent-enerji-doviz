using AutoMapper;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Expense;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Expense;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Office;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Expense;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using BaskentEnerji.Entity.Modals.ViewModals.ExchangeOFfice.Expense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Expense
{
    public class ExpensePaymentService : IExpensePaymentService
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly IMapper _mapper;
        private readonly IVaultService _vaultService;
        private readonly ValidationService _validationService;

        public ExpensePaymentService(
            BaskentEnerjiDbContext context, 
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
                .Include(ed => ed.Category)
                .FirstOrDefaultAsync(ed => ed.Id == request.ExpenseDefinitionId);

            if (definition == null)
                throw new InvalidOperationException("Expense definition not found");

            if (!definition.IsActive)
                throw new InvalidOperationException("Expense definition is not active");

            await _validationService.EnsureNotViewerAsync(definition.OfficeId);

            // Get active vault for the office
            var vault = await _context.Vaults
                .FirstOrDefaultAsync(v => v.OfficeId == definition.OfficeId && v.IsActive);

            if (vault == null)
                throw new InvalidOperationException("No active vault found for this office");

            // Get currency for description
            var currency = await _context.Currencies.FindAsync(request.CurrencyId);

            // Onay eşiği: Owner/Admin her zaman direkt öder. Diğer roller, TRY karşılığı eşiğin
            // üzerindeyse Pending oluşturur — bu yolda kasaya HİÇ dokunulmaz (çift/erken düşüm
            // riskini koddan imkânsız kılar), Owner onayladığında ApproveExpensePaymentAsync düşer.
            var isAdmin = await _validationService.IsAdminAsync();
            decimal amountInTRY = request.Amount;
            if (currency?.CurrencyCode != "TRY")
                amountInTRY = request.Amount * await GetExchangeRateToTRY(definition.OfficeId, request.CurrencyId);

            var requiresApproval = !isAdmin && amountInTRY > FinancialConstants.ExpensePaymentApprovalThresholdTRY;

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
                Status = requiresApproval ? ExpenseStatus.Pending : ExpenseStatus.Paid,
                CreatedByUserId = Guid.Parse(_validationService.GetUserID()),
                CreatedDate = DateTime.UtcNow
            };

            _context.ExpensePayments.Add(payment);

            if (!requiresApproval)
            {
                // VaultService/PartyAccountService'teki UPDLOCK deseniyle tutarlı olması için
                // bakiye kontrolü + kasa güncellemesi tek bir transaction'a alınıyor — aksi halde
                // iki eşzamanlı ödeme aynı bakiyeyi okuyup ikisi de yetersiz kontrolünü geçebilir.
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Check vault balance
                    var hasBalance = await _vaultService.CheckVaultBalanceAsync(vault.Id, request.CurrencyId, request.Amount);
                    if (!hasBalance)
                        throw new InvalidOperationException("Insufficient vault balance");

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
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            return await GetPaymentAsync(payment.Id);
        }

        public async Task<List<vm_expensepayment>> GetPendingApprovalsAsync(Guid? officeId = null)
        {
            var query = _context.ExpensePayments
                .Include(ep => ep.ExpenseDefinition)
                    .ThenInclude(ed => ed.Office)
                .Include(ep => ep.ExpenseDefinition)
                    .ThenInclude(ed => ed.Category)
                .Include(ep => ep.Vault)
                .Include(ep => ep.Currency)
                .Include(ep => ep.CreatedByUser)
                .Include(ep => ep.ApprovedByUser)
                .Where(ep => ep.Status == ExpenseStatus.Pending && !ep.IsDeleted);

            if (officeId.HasValue)
                query = query.Where(ep => ep.ExpenseDefinition.OfficeId == officeId.Value);

            var payments = await query.OrderBy(ep => ep.PaymentDate).ToListAsync();
            return payments.Select(MapToViewModel).ToList();
        }

        public async Task<List<vm_expensepayment>> GetPendingApprovalsForOfficesAsync(List<Guid> officeIds)
        {
            if (officeIds == null || officeIds.Count == 0)
                return new List<vm_expensepayment>();

            var payments = await _context.ExpensePayments
                .Include(ep => ep.ExpenseDefinition)
                    .ThenInclude(ed => ed.Office)
                .Include(ep => ep.ExpenseDefinition)
                    .ThenInclude(ed => ed.Category)
                .Include(ep => ep.Vault)
                .Include(ep => ep.Currency)
                .Include(ep => ep.CreatedByUser)
                .Include(ep => ep.ApprovedByUser)
                .Where(ep => ep.Status == ExpenseStatus.Pending && !ep.IsDeleted
                    && officeIds.Contains(ep.ExpenseDefinition.OfficeId))
                .OrderBy(ep => ep.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToViewModel).ToList();
        }

        public async Task<vm_expensepayment> ApproveExpensePaymentAsync(Guid paymentId, bool approve, string rejectionNote)
        {
            if (!await _validationService.IsOwnerAsync())
                throw new ApiException(HttpStatusCode.Forbidden, "Gider onayı için Owner yetkisi gereklidir.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var payment = await _context.ExpensePayments
                    .Include(ep => ep.ExpenseDefinition).ThenInclude(ed => ed.Office)
                    .Include(ep => ep.ExpenseDefinition).ThenInclude(ed => ed.Category)
                    .Include(ep => ep.Currency)
                    .FirstOrDefaultAsync(ep => ep.Id == paymentId);

                if (payment == null)
                    throw new ApiException(HttpStatusCode.NotFound, "Ödeme kaydı bulunamadı.");

                if (payment.Status != ExpenseStatus.Pending)
                    throw new ApiException(HttpStatusCode.BadRequest, "Bu ödeme onay beklemiyor.");

                var userId = Guid.Parse(_validationService.GetUserID());

                if (approve)
                {
                    // Bakiye bekleme sırasında değişmiş olabilir — oluşturma anındaki stale
                    // bilgiye güvenmek yerine burada tekrar kontrol ediyoruz.
                    var hasBalance = await _vaultService.CheckVaultBalanceAsync(payment.VaultId, payment.CurrencyId, payment.Amount);
                    if (!hasBalance)
                        throw new ApiException(HttpStatusCode.BadRequest, "Kasa bakiyesi yetersiz, onay uygulanamadı.");

                    var vaultUpdate = new rm_updatevaultbalance
                    {
                        vaultId = payment.VaultId,
                        currencyId = payment.CurrencyId,
                        amount = -payment.Amount,
                        description = GetTurkishExpenseDescription(payment.ExpenseDefinition, payment, payment.Currency?.CurrencyCode),
                        isEntireBalance = false,
                        TransactionType = TransactionType.Withdrawal
                    };
                    await _vaultService.UpdateVaultBalanceAsync(vaultUpdate);

                    payment.Status = ExpenseStatus.Paid;
                }
                else
                {
                    payment.Status = ExpenseStatus.Cancelled;
                    payment.RejectionNote = rejectionNote;
                }

                payment.ApprovedByUserId = userId;
                payment.ApprovedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetPaymentAsync(payment.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<vm_expensepayment> GetPaymentAsync(Guid id)
        {
            var payment = await _context.ExpensePayments
                .Include(ep => ep.ExpenseDefinition)
                    .ThenInclude(ed => ed.Office)
                .Include(ep => ep.ExpenseDefinition)
                    .ThenInclude(ed => ed.Category)
                .Include(ep => ep.Vault)
                .Include(ep => ep.Currency)
                .Include(ep => ep.CreatedByUser)
                .Include(ep => ep.DeletedByUser)
                .Include(ep => ep.ApprovedByUser)
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
                .Include(ep => ep.ExpenseDefinition)
                    .ThenInclude(ed => ed.Category)
                .Include(ep => ep.Vault)
                .Include(ep => ep.Currency)
                .Include(ep => ep.CreatedByUser)
                .Include(ep => ep.DeletedByUser)
                .Include(ep => ep.ApprovedByUser)
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
            var definitionOfficeId = await _context.ExpenseDefinitions
                .Where(ed => ed.Id == definitionId)
                .Select(ed => (Guid?)ed.OfficeId)
                .FirstOrDefaultAsync();

            if (definitionOfficeId.HasValue)
                await _validationService.ValidateOfficeAccessAsync(definitionOfficeId.Value);

            var query = _context.ExpensePayments
                .Include(ep => ep.ExpenseDefinition)
                    .ThenInclude(ed => ed.Office)
                .Include(ep => ep.ExpenseDefinition)
                    .ThenInclude(ed => ed.Category)
                .Include(ep => ep.Vault)
                .Include(ep => ep.Currency)
                .Include(ep => ep.CreatedByUser)
                .Include(ep => ep.DeletedByUser)
                .Include(ep => ep.ApprovedByUser)
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

            await _validationService.EnsureNotViewerAsync(payment.ExpenseDefinition.OfficeId);

            // Kasa yalnızca Paid ödemelerde düşülmüştü (Pending hiç dokunmamıştı) — bu yüzden
            // ters kayıt da yalnızca Paid için yapılmalı, aksi halde hiç düşülmemiş bir tutar
            // kasaya hayali olarak eklenir.
            var wasPaid = payment.Status == ExpenseStatus.Paid;

            // Mark as deleted
            payment.IsDeleted = true;
            payment.DeletedReason = reason;
            payment.DeletedByUserId = Guid.Parse(_validationService.GetUserID());
            payment.DeletedDate = DateTime.UtcNow;
            payment.Status = ExpenseStatus.Cancelled;

            if (wasPaid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
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
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            else
            {
                await _context.SaveChangesAsync();
            }

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
                    // Kur tanımsızsa bu kalem toplamdan hariç tutulur (yukarıdaki not)
                    var rate = await TryGetExchangeRateToTRY(officeId, expense.CurrencyId);
                    if (rate.HasValue)
                        totalInTRY += expense.Amount * rate.Value;
                }
            }

            return totalInTRY;
        }

        public async Task<Dictionary<Guid, decimal>> GetExpensesByCategoryAsync(Guid officeId, DateTime startDate, DateTime endDate)
        {
            var baseCurrency = await _context.Currencies.FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");
            if (baseCurrency == null)
                return new Dictionary<Guid, decimal>();

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

            var result = new Dictionary<Guid, decimal>();

            foreach (var expense in expenses)
            {
                var categoryId = expense.ExpenseDefinition.CategoryId;

                decimal amountInTRY;
                if (expense.CurrencyId == baseCurrency.Id)
                {
                    amountInTRY = expense.Amount;
                }
                else
                {
                    var rate = await TryGetExchangeRateToTRY(officeId, expense.CurrencyId);
                    if (!rate.HasValue) continue; // Kur tanımsız — bu kalem kategori kırılımından hariç
                    amountInTRY = expense.Amount * rate.Value;
                }

                if (result.ContainsKey(categoryId))
                    result[categoryId] += amountInTRY;
                else
                    result[categoryId] = amountInTRY;
            }

            return result;
        }

        // Kur bulunamadığında sessizce 0 dönmek, TRY karşılığını 0 gösterir — bu hem onay eşiği
        // kontrolünü (CreatePaymentAsync) bypass eder (büyük yabancı para tutarı hiç onaysız
        // geçebilir) hem de bütçe ekranında gerçek harcamayı gizler. Bu yüzden kur çözümlenemezse
        // açıkça hata fırlatılır; ödeme oluşturma akışı bunu kullanıcıya net bir mesajla iletir.
        private async Task<decimal> GetExchangeRateToTRY(Guid officeId, Guid currencyId)
        {
            var baseCurrency = await _context.Currencies
                .FirstOrDefaultAsync(c => c.CurrencyCode == "TRY");

            if (baseCurrency == null || currencyId == baseCurrency.Id)
                return 1m;

            var currencyCode = await _context.Currencies
                .Where(c => c.Id == currencyId)
                .Select(c => c.CurrencyCode)
                .FirstOrDefaultAsync();

            var rate = await _context.ExchangeRates
                .Where(r => r.OfficeId == officeId &&
                           r.SourceCurrencyId == currencyId &&
                           r.TargetCurrencyId == baseCurrency.Id &&
                           r.IsActive)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (rate == null || rate.SellRate <= 0)
                throw new InvalidOperationException(
                    $"{currencyCode ?? "Seçilen para birimi"} için bu ofiste tanımlı bir kur bulunamadı. " +
                    "Devam etmeden önce Manuel Kur Yönetimi'nden bu para birimi için TRY kuru tanımlayın.");

            return rate.SellRate;
        }

        // Bütçe/harcama özeti gibi salt-okunur toplu raporlama ekranlarında tek bir para biriminin
        // kuru eksik diye tüm sayfayı çökertmek yerine, o kalemi TRY toplamından hariç tutuyoruz —
        // yazma (ödeme oluşturma) akışındaki gibi sert bir engel burada uygun değil.
        private async Task<decimal?> TryGetExchangeRateToTRY(Guid officeId, Guid currencyId)
        {
            try
            {
                return await GetExchangeRateToTRY(officeId, currencyId);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private string GeneratePaymentNumber()
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = RandomNumberGenerator.GetInt32(10000, 100000);
            return $"EXP-{date}-{random}";
        }

        private string GetTurkishExpenseDescription(ExpenseDefinition definition, ExpensePayment payment, string currencyCode)
        {
            var paymentMethod = GetTurkishPaymentMethod(payment.PaymentMethod);

            // Example: "Ahmet'e 30000 TRY maaş ödemesi yapıldı"
            if (definition.Category?.Name == "Maaş")
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

        private vm_expensepayment MapToViewModel(ExpensePayment payment)
        {
            return new vm_expensepayment
            {
                Id = payment.Id,
                ExpenseDefinitionId = payment.ExpenseDefinitionId,
                ExpenseDefinitionName = payment.ExpenseDefinition?.Name,
                ExpenseDefinitionCode = payment.ExpenseDefinition?.Code,
                CategoryId = payment.ExpenseDefinition?.CategoryId ?? Guid.Empty,
                CategoryName = payment.ExpenseDefinition?.Category?.Name,
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
                CreatedByUserName = payment.CreatedByUser?.Username,
                ApprovedByUserName = payment.ApprovedByUser?.Username,
                ApprovedAt = payment.ApprovedAt,
                RejectionNote = payment.RejectionNote
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