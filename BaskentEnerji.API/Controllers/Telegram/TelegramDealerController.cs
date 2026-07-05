using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BaskentEnerji.API.Controllers.Telegram
{
    [Route("api/v1/tg/dealer")]
    [ApiController]
    [Authorize]
    public class TelegramDealerController : ControllerBase
    {
        private readonly BaskentEnerjiDbContext _db;
        private readonly ValidationService _validationService;

        public TelegramDealerController(BaskentEnerjiDbContext db, ValidationService validationService)
        {
            _db = db;
            _validationService = validationService;
        }

        private async Task RequireStaff()
        {
            if (!await _validationService.IsStaff())
                throw new ApiException(HttpStatusCode.Forbidden, "Staff yetkisi gereklidir.");
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            await RequireStaff();

            var userIdStr = _validationService.GetUserID();
            if (!Guid.TryParse(userIdStr, out var userId))
                throw new ApiException(HttpStatusCode.Unauthorized, "Geçersiz kullanıcı");

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                throw new ApiException(HttpStatusCode.NotFound, "Kullanıcı bulunamadı");

            var dealerCode = user.DealerReferralCode ?? "";

            var tgDealer = await _db.TgDealers
                .FirstOrDefaultAsync(d => d.DealerCode == dealerCode);

            var txs = await _db.TgTransactions
                .Where(t => t.ReferralCode == dealerCode)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    Id = t.TransactionId,
                    t.CustomerId,
                    t.Currency,
                    t.Amount,
                    t.ExchangeRate,
                    TlAmount = t.TryAmount,
                    t.Status,
                    t.IsBuy,
                    t.CreatedAt,
                    t.CompletedAt
                })
                .ToListAsync();

            var completedTxs = txs.Where(t => t.Status?.ToLower() == "completed").ToList();
            var givenTl = completedTxs.Sum(t => (decimal?)t.TlAmount ?? 0);
            var usdtTotal = completedTxs.Where(t => t.Currency?.ToUpper() == "USDT").Sum(t => (decimal?)t.Amount ?? 0);
            var rubTotal = completedTxs.Where(t => t.Currency?.ToUpper() == "RUB" || t.Currency?.ToUpper() == "RUBLE").Sum(t => (decimal?)t.Amount ?? 0);

            var cryptoTxs = txs.Where(t => t.Currency?.ToUpper() == "USDT").ToList();
            var cryptoVerified = completedTxs.Where(t => t.Currency?.ToUpper() == "USDT").Count();

            var dealerName = tgDealer?.DealerName ?? $"{user.Firstname} {user.Lastname}".Trim();
            var dealerBalance = tgDealer?.Balance ?? 0;

            return Ok(new
            {
                balance = dealerBalance,
                given_tl = givenTl,
                usdt = usdtTotal,
                rub = rubTotal,
                dealer_name = dealerName,
                dealer_code = dealerCode,
                staff_name = $"{user.Firstname} {user.Lastname}".Trim(),
                transactions = txs,
                crypto_summary = new
                {
                    total_usdt_tx = cryptoTxs.Count,
                    completed_usdt_tx = cryptoVerified,
                    total_usdt_amount = usdtTotal
                },
                dealer = new
                {
                    Id = tgDealer?.DealerId ?? 0,
                    Name = dealerName,
                    DealerCode = dealerCode,
                    IsActive = tgDealer?.IsActive ?? (user.Rank != Entity.Rank.Banned),
                    Balance = dealerBalance,
                    user.CreatedDate
                }
            });
        }

        [HttpGet("crypto-deposits")]
        public async Task<IActionResult> CryptoDeposits()
        {
            await RequireStaff();

            var userIdStr = _validationService.GetUserID();
            if (!Guid.TryParse(userIdStr, out var userId))
                throw new ApiException(HttpStatusCode.Unauthorized, "Geçersiz kullanıcı");

            var user = await _db.Users.FindAsync(userId);
            var dealerCode = user?.DealerReferralCode ?? "";

            var txIds = await _db.TgTransactions
                .Where(t => t.ReferralCode == dealerCode)
                .Select(t => t.TransactionId)
                .ToListAsync();

            var deposits = await _db.TgCryptoDeposits
                .Where(d => d.TransactionId.HasValue && txIds.Contains(d.TransactionId.Value))
                .OrderByDescending(d => d.DepositTime)
                .Select(d => new
                {
                    Id = d.DepositId,
                    d.TransactionId,
                    d.Txid,
                    d.Amount,
                    d.Network,
                    Address = d.ToAddress,
                    d.Confirmations,
                    d.Status,
                    CreatedAt = d.DepositTime
                })
                .ToListAsync();

            return Ok(new { deposits });
        }
    }
}
