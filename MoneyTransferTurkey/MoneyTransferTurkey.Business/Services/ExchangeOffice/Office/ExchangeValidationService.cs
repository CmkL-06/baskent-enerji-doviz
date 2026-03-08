using Microsoft.EntityFrameworkCore;
using MoneyTransferTurkey.Business.Infrastructure.ExchangeOffice.Office;
using MoneyTransferTurkey.Data.Contexts;
using MoneyTransferTurkey.Entity.Modals.RequestModals.ExchangeService.Office;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Services.ExchangeOffice.Office
{
    public class ExchangeValidationService : IExchangeValidationService
    {
        private readonly MoneyTransferTurkeyDbContext _context;
        private readonly IVaultService _vaultService;

        public ExchangeValidationService(MoneyTransferTurkeyDbContext context, IVaultService vaultService)
        {
            _context = context;
            _vaultService = vaultService;
        }

        public async Task<ExchangeValidationResult> ValidateExchangeTransaction(rm_exchangetransaction request)
        {
            var result = new ExchangeValidationResult();

            // Validate vault exists
            var vault = await _context.Vaults.FindAsync(request.VaultId);
            if (vault == null || !vault.IsActive)
            {
                result.AddError("Invalid vault");
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