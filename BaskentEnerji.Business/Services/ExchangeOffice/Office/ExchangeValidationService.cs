using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Office;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.Office
{
    public class ExchangeValidationService : IExchangeValidationService
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly IVaultService _vaultService;
        private readonly IWacService _wacService;
        private readonly IDayClosureService _dayClosureService;
        private readonly ValidationService _validationService;

        public ExchangeValidationService(
            BaskentEnerjiDbContext context,
            IVaultService vaultService,
            IWacService wacService,
            IDayClosureService dayClosureService,
            ValidationService validationService)
        {
            _context = context;
            _vaultService = vaultService;
            _wacService = wacService;
            _dayClosureService = dayClosureService;
            _validationService = validationService;
        }

        public async Task<ExchangeValidationResult> ValidateExchangeTransaction(rm_exchangetransaction request)
        {
            var result = new ExchangeValidationResult();

            // Day closure check
            var vault = await _context.Vaults.FindAsync(request.VaultId);
            if (vault == null || !vault.IsActive)
            {
                result.AddError("Invalid vault");
                return result;
            }

            var canTransact = await _dayClosureService.CanTransactAsync(vault.OfficeId);
            if (!canTransact)
            {
                var dayStatus = await _dayClosureService.GetDayStatusAsync(vault.OfficeId);
                result.AddError(dayStatus.BlockReason ?? "Önceki gün kapanışı yapılmadı. İşlem yapabilmek için gün kapanışı gerekli.");
                return result;
            }

            // Validate currencies exist
            var sourceCurrency = await _context.Currencies.FindAsync(request.SourceCurrencyId);
            var targetCurrency = await _context.Currencies.FindAsync(request.TargetCurrencyId);

            if (sourceCurrency == null || targetCurrency == null)
            {
                result.AddError("Invalid currency");
                return result;
            }

            // Validate amount
            if (request.SourceAmount <= 0)
            {
                result.AddError("Amount must be greater than zero");
                return result;
            }

            // Arbitraj (çapraz kur): her iki taraf da TRY değil — tek bir SourceCurrencyId→TargetCurrencyId
            // ExchangeRate çifti yok, kur/bakiye/WAC doğrulaması ProcessExchangeAsync içinde (iki bağımsız
            // kur ve iki bağımsız bacak ile) yapılıyor; burada sadece manuel kurların girildiğini doğrula.
            if (sourceCurrency.CurrencyCode != "TRY" && targetCurrency.CurrencyCode != "TRY")
            {
                if (!request.SourceCustomRate.HasValue || !request.TargetCustomRate.HasValue)
                {
                    result.AddError("Arbitraj işlemi için hem alınan hem verilen birimin kuru girilmelidir.");
                }
                return result;
            }

            // Validate exchange rate exists
            var rate = await _context.ExchangeRates
                .FirstOrDefaultAsync(r =>
                    r.SourceCurrencyId == request.SourceCurrencyId &&
                    r.TargetCurrencyId == request.TargetCurrencyId &&
                    r.IsActive);

            if (rate == null)
            {
                result.AddError("Exchange rate not available for this currency pair");
                return result;
            }

            // WAC validation for sell transactions (office selling foreign currency)
            if (!request.IsBuyingFromCustomer)
            {
                var sellRate = request.CustomRate ?? rate.SellRate;
                var wac = await _wacService.GetWacAsync(request.VaultId, request.SourceCurrencyId);

                if (wac > 0 && sellRate < wac)
                {
                    var loss = (wac - sellRate) * request.SourceAmount;
                    if (!request.OwnerOverrideLoss)
                    {
                        var isOwner = await _validationService.IsOwnerAsync();
                        if (!isOwner)
                        {
                            result.AddError($"Satış kuru ({sellRate:F4}) maliyetin ({wac:F4}) altında. Tahmini zarar: {loss:F2} TL. Sadece Patron onaylayabilir.");
                        }
                        else
                        {
                            result.AddWarning($"UYARI: Maliyetin altında satış. Zarar: {loss:F2} TL.");
                        }
                    }
                    else
                    {
                        result.AddWarning($"Patron onayı ile zarar satışı: {loss:F2} TL.");
                    }
                }
            }

            // Validate vault balance
            var requiredCurrencyId = request.IsBuyingFromCustomer
                ? request.TargetCurrencyId
                : request.SourceCurrencyId;

            var hasBalance = await _vaultService.CheckVaultBalanceAsync(
                request.VaultId,
                requiredCurrencyId,
                request.SourceAmount * (request.IsBuyingFromCustomer ? rate.BuyRate : 1)
            );

            if (!hasBalance)
            {
                result.AddError("Insufficient vault balance");
            }

            return result;
        }

        public async Task<ExchangeValidationResult> ValidateTransfer(rm_transferbetweenvaults request)
        {
            var result = new ExchangeValidationResult();

            // Validate vaults
            var sourceVault = await _context.Vaults.FindAsync(request.SourceVaultId);
            var targetVault = await _context.Vaults.FindAsync(request.TargetVaultId);

            if (sourceVault == null || !sourceVault.IsActive)
            {
                result.AddError("Invalid source vault");
            }

            if (targetVault == null || !targetVault.IsActive)
            {
                result.AddError("Invalid target vault");
            }

            if (request.SourceVaultId == request.TargetVaultId)
            {
                result.AddError("Cannot transfer to same vault");
            }

            // Day closure check for source vault
            if (sourceVault != null)
            {
                var canTransact = await _dayClosureService.CanTransactAsync(sourceVault.OfficeId);
                if (!canTransact)
                {
                    result.AddError("Önceki gün kapanışı yapılmadı. Transfer yapabilmek için gün kapanışı gerekli.");
                    return result;
                }
            }

            // Validate currency
            var currency = await _context.Currencies.FindAsync(request.CurrencyId);
            if (currency == null)
            {
                result.AddError("Invalid currency");
            }

            // Validate amount
            if (request.Amount <= 0)
            {
                result.AddError("Amount must be greater than zero");
            }

            // Validate balance
            if (result.IsValid)
            {
                var hasBalance = await _vaultService.CheckVaultBalanceAsync(
                    request.SourceVaultId,
                    request.CurrencyId,
                    request.Amount
                );

                if (!hasBalance)
                {
                    result.AddError("Insufficient balance in source vault");
                }
            }

            return result;
        }
    }
}
