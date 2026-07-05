using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.Telegram;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BaskentEnerji.API.Controllers.Telegram
{
    [Route("api/v1/tg/admin")]
    [ApiController]
    [Authorize]
    public class TelegramAdminController : ControllerBase
    {
        private readonly BaskentEnerjiDbContext _db;
        private readonly ValidationService _validationService;

        public TelegramAdminController(BaskentEnerjiDbContext db, ValidationService validationService)
        {
            _db = db;
            _validationService = validationService;
        }

        private async Task RequireAdmin()
        {
            if (!await _validationService.IsAdminAsync())
                throw new ApiException(HttpStatusCode.Forbidden, "Admin yetkisi gereklidir.");
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            await RequireAdmin();

            var totalTx = await _db.TgTransactions.CountAsync();
            var completedTx = _db.TgTransactions.Where(t => t.Status == "completed");
            var totalTl = await completedTx.SumAsync(t => (decimal?)t.TryAmount ?? 0);
            var totalUsdt = await completedTx.Where(t => t.Currency == "USDT").SumAsync(t => (decimal?)t.Amount ?? 0);
            var totalRub = await completedTx.Where(t => t.Currency == "RUBLE" || t.Currency == "RUB").SumAsync(t => (decimal?)t.Amount ?? 0);

            var totalDealerBalance = await _db.TgDealers.Where(d => d.IsActive).SumAsync(d => (decimal?)d.Balance ?? 0);
            var totalCustomers = await _db.TgCustomers.CountAsync();
            var pendingTx = await _db.TgTransactions.CountAsync(t => t.Status == "pending" || t.Status == "processing");
            var completedTxCount = await completedTx.CountAsync();

            var recentTx = await _db.TgTransactions
                .Include(t => t.Customer)
                .OrderByDescending(t => t.CreatedAt)
                .Take(15)
                .Select(t => new
                {
                    t.TransactionId,
                    t.Currency,
                    t.Amount,
                    TlAmount = t.TryAmount,
                    t.ExchangeRate,
                    t.Status,
                    t.IsBuy,
                    t.CreatedAt,
                    t.CompletedAt,
                    CustomerName = t.Customer != null ? t.Customer.FirstName : null,
                    t.ReferralCode
                })
                .ToListAsync();

            return Ok(new
            {
                total_tx = totalTx,
                total_tl = totalTl,
                total_usdt = totalUsdt,
                total_rub = totalRub,
                total_dealer_balance = totalDealerBalance,
                total_customers = totalCustomers,
                pending_tx = pendingTx,
                completed_tx = completedTxCount,
                recent_transactions = recentTx
            });
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> Transactions()
        {
            await RequireAdmin();

            var txs = await _db.TgTransactions
                .Include(t => t.Customer)
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
                    t.ReferralCode,
                    t.AssignedOperatorId,
                    t.IsBuy,
                    t.CreatedAt,
                    t.CompletedAt,
                    t.CompletionCode,
                    CustomerName = t.Customer != null ? t.Customer.FirstName : null,
                    CustomerUsername = t.Customer != null ? t.Customer.Username : null
                })
                .ToListAsync();

            return Ok(new { transactions = txs });
        }

        [HttpGet("dealers")]
        public async Task<IActionResult> GetDealers()
        {
            await RequireAdmin();

            var dealerUsers = await _db.Users
                .Where(u => u.DealerReferralCode != null)
                .Select(u => new
                {
                    id = u.Id,
                    username = u.Username,
                    name = (u.Firstname + " " + u.Lastname).Trim(),
                    dealer_code = u.DealerReferralCode,
                    is_active = u.Rank != Entity.Rank.Banned,
                    created_at = u.CreatedDate
                })
                .ToListAsync();

            var tgDealers = await _db.TgDealers.ToListAsync();
            var txCounts = await _db.TgTransactions
                .Where(t => t.ReferralCode != null)
                .GroupBy(t => t.ReferralCode)
                .Select(g => new { Code = g.Key, Total = g.Count(), Completed = g.Count(t => t.Status == "completed") })
                .ToListAsync();

            var dealers = dealerUsers.Select(u =>
            {
                var tgd = tgDealers.FirstOrDefault(d => d.DealerCode == u.dealer_code);
                var txc = txCounts.FirstOrDefault(t => t.Code == u.dealer_code);
                return new
                {
                    u.id,
                    u.username,
                    u.name,
                    u.dealer_code,
                    dealer_name = tgd?.DealerName ?? u.name,
                    u.is_active,
                    balance = tgd?.Balance ?? 0,
                    total_tx = txc?.Total ?? 0,
                    completed_tx = txc?.Completed ?? 0,
                    u.created_at
                };
            }).ToList();

            return Ok(new { dealers });
        }

        [HttpPost("dealers")]
        public async Task<IActionResult> CreateDealer([FromBody] CreateDealerRequest req)
        {
            await RequireAdmin();

            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Name))
                throw new ApiException(HttpStatusCode.BadRequest, "Kullanıcı adı ve ad gerekli");

            var rng = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var dealerCode = new string(Enumerable.Range(0, 6).Select(_ => chars[rng.Next(chars.Length)]).ToArray());

            var existing = await _db.Users.AnyAsync(u => u.DealerReferralCode == dealerCode);
            if (existing)
                dealerCode = new string(Enumerable.Range(0, 6).Select(_ => chars[rng.Next(chars.Length)]).ToArray());

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user != null)
            {
                user.DealerReferralCode = dealerCode;
                _db.Users.Update(user);
            }
            else
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Kullanıcı bulunamadı. Önce sistem kullanıcısı oluşturun.");
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true, dealer_code = dealerCode });
        }

        [HttpGet("operators")]
        public async Task<IActionResult> GetOperators()
        {
            await RequireAdmin();

            var operators = await _db.Users
                .Where(u => u.TelegramOperatorId != null)
                .Select(u => new
                {
                    id = u.Id,
                    username = u.Username,
                    name = u.Firstname + " " + u.Lastname,
                    telegram_id = u.TelegramOperatorId,
                    is_active = u.Rank != Entity.Rank.Banned,
                    created_at = u.CreatedDate
                })
                .ToListAsync();

            return Ok(new { operators });
        }

        [HttpPost("operators")]
        public async Task<IActionResult> CreateOperator([FromBody] CreateOperatorRequest req)
        {
            await RequireAdmin();

            if (string.IsNullOrWhiteSpace(req.Username))
                throw new ApiException(HttpStatusCode.BadRequest, "Kullanıcı adı gerekli");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user == null)
                throw new ApiException(HttpStatusCode.BadRequest, "Kullanıcı bulunamadı.");

            user.TelegramOperatorId = req.TelegramId;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpDelete("operators/{userId}")]
        public async Task<IActionResult> DeleteOperator(Guid userId)
        {
            await RequireAdmin();

            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.TelegramOperatorId = null;
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }

        [HttpDelete("dealers/{userId}")]
        public async Task<IActionResult> DeleteDealer(Guid userId)
        {
            await RequireAdmin();

            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.DealerReferralCode = null;
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }

        [HttpGet("crypto-deposits")]
        public async Task<IActionResult> CryptoDeposits()
        {
            await RequireAdmin();

            var deposits = await _db.TgCryptoDeposits
                .OrderByDescending(d => d.DepositTime)
                .Select(d => new
                {
                    Id = d.DepositId,
                    d.TransactionId,
                    d.DealerId,
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

        [HttpGet("bot-status")]
        public async Task<IActionResult> BotStatus()
        {
            await RequireAdmin();

            var heartbeats = await _db.TgBotHeartbeats.ToListAsync();
            var result = heartbeats.ToDictionary(
                h => h.BotName,
                h =>
                {
                    var ageSeconds = h.LastHeartbeat.HasValue
                        ? (DateTime.Now - h.LastHeartbeat.Value).TotalSeconds
                        : (double?)null;
                    return new
                    {
                        last_heartbeat = h.LastHeartbeat,
                        last_transaction_at = h.LastTransactionAt,
                        active_sessions = h.ActiveSessions ?? 0,
                        status = ageSeconds.HasValue && ageSeconds < 120 ? "online" : "offline",
                        age_seconds = ageSeconds.HasValue ? (int)ageSeconds : (int?)null
                    };
                });

            var expected = new[] { "main_bot", "operator_bot", "ruble_bot" };
            foreach (var name in expected)
            {
                if (!result.ContainsKey(name))
                    result[name] = new { last_heartbeat = (DateTime?)null, last_transaction_at = (DateTime?)null, active_sessions = 0, status = "unknown", age_seconds = (int?)null };
            }

            return Ok(result);
        }

        [HttpGet("baskent-queue")]
        public async Task<IActionResult> BaskentQueue()
        {
            await RequireAdmin();

            var items = await _db.TgApiQueue
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            var pending = items.Count(i => i.Status == "pending");
            var failed = items.Count(i => i.Status == "failed");
            var success = items.Count(i => i.Status == "success");

            return Ok(new { items, summary = new { pending, failed, success } });
        }

        [HttpPost("baskent-queue/{queueId}/retry")]
        public async Task<IActionResult> RetryQueue(int queueId)
        {
            await RequireAdmin();

            var item = await _db.TgApiQueue.FindAsync(queueId);
            if (item != null)
            {
                item.Status = "pending";
                item.Attempts = 0;
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }

        [HttpGet("login-logs")]
        public async Task<IActionResult> LoginLogs()
        {
            await RequireAdmin();

            var logs = await _db.TgLoginLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToListAsync();

            return Ok(new { logs });
        }

        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            await RequireAdmin();

            var today = DateTime.Today;
            var weekAgo = DateTime.Now.AddDays(-7);
            var monthAgo = DateTime.Now.AddDays(-30);

            var todayCount = await _db.TgTransactions.CountAsync(t => t.CreatedAt >= today);
            var weekCount = await _db.TgTransactions.CountAsync(t => t.CreatedAt >= weekAgo);
            var monthCount = await _db.TgTransactions.CountAsync(t => t.CreatedAt >= monthAgo);
            var activeDealers = await _db.TgTransactions
                .Where(t => t.ReferralCode != null)
                .Select(t => t.ReferralCode)
                .Distinct()
                .CountAsync();

            var recentLogins = await _db.TgLoginLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(10)
                .ToListAsync();

            return Ok(new
            {
                today_count = todayCount,
                week_count = weekCount,
                month_count = monthCount,
                active_dealers = activeDealers,
                recentLogins
            });
        }
        [HttpGet("exchange-rates")]
        public async Task<IActionResult> ExchangeRates()
        {
            await RequireAdmin();
            var rates = await _db.TgExchangeRates.ToListAsync();
            return Ok(new { rates });
        }

        [HttpPut("exchange-rates/{id}")]
        public async Task<IActionResult> UpdateExchangeRate(int id, [FromBody] UpdateRateRequest req)
        {
            await RequireAdmin();

            var rate = await _db.TgExchangeRates.FindAsync(id);
            if (rate == null)
                throw new ApiException(HttpStatusCode.NotFound, "Kur bulunamadı");

            rate.BuyRate = req.BuyRate;
            rate.SellRate = req.SellRate;
            rate.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, rate });
        }

        [HttpPost("exchange-rates")]
        public async Task<IActionResult> CreateExchangeRate([FromBody] CreateRateRequest req)
        {
            await RequireAdmin();

            if (string.IsNullOrWhiteSpace(req.Currency))
                throw new ApiException(HttpStatusCode.BadRequest, "Para birimi gerekli");

            var existing = await _db.TgExchangeRates.FirstOrDefaultAsync(r => r.Currency == req.Currency.ToUpper());
            if (existing != null)
                throw new ApiException(HttpStatusCode.Conflict, "Bu para birimi zaten mevcut");

            var rate = new Entity.Entities.Telegram.TgExchangeRate
            {
                Currency = req.Currency.ToUpper(),
                BuyRate = req.BuyRate,
                SellRate = req.SellRate,
                UpdatedAt = DateTime.Now
            };
            _db.TgExchangeRates.Add(rate);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, rate });
        }

        [HttpGet("crypto-summary")]
        public async Task<IActionResult> CryptoSummary()
        {
            await RequireAdmin();

            var deposits = await _db.TgCryptoDeposits.ToListAsync();
            var transactions = await _db.TgTransactions
                .Where(t => t.Currency == "USDT")
                .ToListAsync();

            var totalDeposits = deposits.Sum(d => d.Amount);
            var pendingDeposits = deposits.Where(d => d.Status == "pending").Sum(d => d.Amount);
            var confirmedDeposits = deposits.Where(d => d.Status == "confirmed" || d.Status == "completed").Sum(d => d.Amount);
            var pendingCount = deposits.Count(d => d.Status == "pending");
            var confirmedCount = deposits.Count(d => d.Status == "confirmed" || d.Status == "completed");

            var verifiedTx = transactions.Count(t => t.CryptoVerified == true);
            var unverifiedTx = transactions.Count(t => t.Txid != null && t.CryptoVerified != true);

            return Ok(new
            {
                total_deposits = totalDeposits,
                pending_deposits = pendingDeposits,
                confirmed_deposits = confirmedDeposits,
                pending_count = pendingCount,
                confirmed_count = confirmedCount,
                verified_transactions = verifiedTx,
                unverified_transactions = unverifiedTx,
                total_usdt_transactions = transactions.Count
            });
        }
    }

    public class UpdateRateRequest
    {
        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }
    }

    public class CreateRateRequest
    {
        public string Currency { get; set; } = "";
        public decimal BuyRate { get; set; }
        public decimal SellRate { get; set; }
    }

    public class CreateDealerRequest
    {
        public string Username { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public class CreateOperatorRequest
    {
        public string Username { get; set; } = "";
        public long? TelegramId { get; set; }
    }
}
