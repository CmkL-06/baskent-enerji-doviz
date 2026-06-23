using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MoneyTransfer.API.Data;
using MoneyTransfer.API.Helpers;
using MoneyTransfer.API.Models;

namespace MoneyTransfer.API.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var grp = app.MapGroup("/api/admin").RequireAuthorization();

        // Admin yetkisi kontrolü
        bool IsAdmin(ClaimsPrincipal u) =>
            u.FindFirstValue(ClaimTypes.Role) == "admin" ||
            u.FindFirstValue("is_admin") == "true";

        // ── DASHBOARD ────────────────────────────────────────────────────────
        grp.MapGet("/dashboard", async (ClaimsPrincipal user, MttDbContext db) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();

            var totalTx      = await db.Transactions.CountAsync();
            var totalTl      = await db.Transactions.Where(t => t.Status == "completed").SumAsync(t => t.TlAmount);
            var totalUsdt    = await db.Transactions.Where(t => t.Status == "completed" && t.Currency == "USDT").SumAsync(t => t.Amount);
            var totalRub     = await db.Transactions.Where(t => t.Status == "completed" && t.Currency == "RUB").SumAsync(t => t.Amount);
            var totalBalance = await db.Dealers.SumAsync(d => d.Balance);

            var recent = await db.Transactions
                .Include(t => t.Dealer)
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .Select(t => new { t.Id, t.CustomerName, t.Amount, t.Currency, tl_amount = t.TlAmount, t.Rate, t.Status, dealer_name = t.Dealer.Name, created_at = t.CreatedAt })
                .ToListAsync();

            return Results.Ok(new
            {
                total_tx               = totalTx,
                total_tl               = totalTl,
                total_usdt             = totalUsdt,
                total_rub              = totalRub,
                total_dealer_balance   = totalBalance,
                recent_transactions    = recent
            });
        });

        // ── TRANSACTIONS ─────────────────────────────────────────────────────
        grp.MapGet("/transactions", async (ClaimsPrincipal user, MttDbContext db) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();
            var list = await db.Transactions
                .Include(t => t.Dealer).Include(t => t.Operator)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id, t.CustomerName, t.Amount, t.Currency,
                    tl_amount     = t.TlAmount,
                    t.Rate, t.Status,
                    code          = t.Code,
                    dealer_name   = t.Dealer.Name,
                    operator_name = t.Operator != null ? t.Operator.Name : null,
                    created_at    = t.CreatedAt
                })
                .ToListAsync();
            return Results.Ok(new { transactions = list });
        });

        // ── OPERATORS ────────────────────────────────────────────────────────
        grp.MapGet("/operators", async (ClaimsPrincipal user, MttDbContext db) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();
            var list = await db.Operators
                .Include(o => o.User)
                .Select(o => new
                {
                    o.Id, username = o.User.Username, o.Name,
                    telegram_id = o.TelegramId, o.IsAdmin, is_active = o.IsActive
                })
                .ToListAsync();
            return Results.Ok(new { operators = list });
        });

        grp.MapPost("/operators", async (ClaimsPrincipal user, CreateOperatorRequest req, MttDbContext db) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();
            if (await db.Users.AnyAsync(u => u.Username == req.Username))
                return Results.Json(new { success = false, message = "Bu kullanıcı adı zaten kullanımda." }, statusCode: 409);

            var newUser = new User
            {
                Username     = req.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                Name         = req.Name,
                Role         = "operator",
                TelegramId   = req.TelegramId
            };
            db.Users.Add(newUser);
            await db.SaveChangesAsync();

            db.Operators.Add(new Operator { UserId = newUser.Id, Name = req.Name, TelegramId = req.TelegramId, IsAdmin = req.IsAdmin });
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        grp.MapDelete("/operators/{id:int}", async (int id, ClaimsPrincipal user, MttDbContext db) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();
            var op = await db.Operators.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == id);
            if (op is null) return Results.NotFound();
            op.IsActive = op.User.IsActive = false;
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // ── DEALERS ──────────────────────────────────────────────────────────
        grp.MapGet("/dealers", async (ClaimsPrincipal user, MttDbContext db) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();
            var list = await db.Dealers
                .Include(d => d.User)
                .Select(d => new
                {
                    d.Id, username = d.User.Username, d.Name,
                    d.Balance, vault_id = d.VaultId,
                    telegram_id = d.TelegramId, is_active = d.IsActive,
                    dealer_code = d.DealerCode
                })
                .ToListAsync();
            return Results.Ok(new { dealers = list });
        });

        grp.MapPost("/dealers", async (ClaimsPrincipal user, CreateDealerRequest req, MttDbContext db) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();
            if (await db.Users.AnyAsync(u => u.Username == req.Username))
                return Results.Json(new { success = false, message = "Bu kullanıcı adı zaten kullanımda." }, statusCode: 409);

            var newUser = new User
            {
                Username     = req.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                Name         = req.Name,
                Role         = "dealer",
                TelegramId   = req.TelegramId
            };
            db.Users.Add(newUser);
            await db.SaveChangesAsync();

            // Benzersiz dealer kodu
            string code;
            do { code = JwtHelper.GenerateCode(8); }
            while (await db.Dealers.AnyAsync(d => d.DealerCode == code));

            db.Dealers.Add(new Dealer
            {
                UserId     = newUser.Id,
                Name       = req.Name,
                Balance    = req.Balance,
                VaultId    = req.VaultId,
                DealerCode = code,
                TelegramId = req.TelegramId
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        grp.MapDelete("/dealers/{id:int}", async (int id, ClaimsPrincipal user, MttDbContext db) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();
            var dealer = await db.Dealers.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
            if (dealer is null) return Results.NotFound();
            dealer.IsActive = dealer.User.IsActive = false;
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // ── RATES ────────────────────────────────────────────────────────────
        grp.MapGet("/rates", async (ClaimsPrincipal user, MttDbContext db) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();
            var rates = await db.ExchangeRates.ToListAsync();
            var usdt  = rates.FirstOrDefault(r => r.Currency == "USDT");
            var rub   = rates.FirstOrDefault(r => r.Currency == "RUB");
            return Results.Ok(new
            {
                usdt_buy     = usdt?.BuyRate,  usdt_sell = usdt?.SellRate, usdt_updated = usdt?.UpdatedAt,
                rub_buy      = rub?.BuyRate,   rub_sell  = rub?.SellRate,  rub_updated  = rub?.UpdatedAt
            });
        });

        grp.MapPost("/rates", async (ClaimsPrincipal user, UpdateRateRequest req, MttDbContext db) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();
            var rate = await db.ExchangeRates.FirstOrDefaultAsync(r => r.Currency == req.Currency.ToUpper());
            if (rate is null) return Results.NotFound();
            rate.BuyRate   = req.Buy;
            rate.SellRate  = req.Sell;
            rate.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // Binance'den güncel kur al
        grp.MapGet("/rates/fetch", async (string currency, ClaimsPrincipal user, IHttpClientFactory http) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();
            var cur = currency.ToUpper();
            try
            {
                var client = http.CreateClient();
                if (cur == "USDT")
                {
                    // USDTTRY paritesi Binance'den
                    var r = await client.GetFromJsonAsync<BinanceTicker>(
                        "https://api.binance.com/api/v3/ticker/price?symbol=USDTTRY");
                    if (r?.Price is not null)
                    {
                        var mid = decimal.Parse(r.Price, System.Globalization.CultureInfo.InvariantCulture);
                        return Results.Ok(new { buy = Math.Round(mid * 0.99m, 4), sell = Math.Round(mid * 1.01m, 4) });
                    }
                }
                else if (cur == "RUB")
                {
                    // USD/RUB → USD/TRY oranından hesapla
                    var rubUsd = await client.GetFromJsonAsync<ExchangeRateResponse>(
                        "https://open.er-api.com/v6/latest/USD");
                    if (rubUsd?.Rates is not null &&
                        rubUsd.Rates.TryGetValue("RUB", out var usdRub) &&
                        rubUsd.Rates.TryGetValue("TRY", out var usdTry))
                    {
                        var rubTry = usdTry / usdRub;
                        return Results.Ok(new { buy = Math.Round(rubTry * 0.98m, 6), sell = Math.Round(rubTry * 1.02m, 6) });
                    }
                }
            }
            catch { /* API hatası loglanır */ }

            return Results.Json(new { success = false, message = "API'den kur alınamadı." }, statusCode: 503);
        });

        // ── CRYPTO DEPOSITS ──────────────────────────────────────────────────
        grp.MapGet("/crypto-deposits", async (ClaimsPrincipal user, MttDbContext db) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();
            var list = await db.CryptoDeposits
                .Include(c => c.Dealer)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id, c.Txid, c.Network, c.Amount, c.Address, c.Tag,
                    dealer_name = c.Dealer != null ? c.Dealer.Name : null,
                    c.Status, created_at = c.CreatedAt
                })
                .ToListAsync();
            return Results.Ok(new { deposits = list });
        });

        grp.MapPost("/crypto-deposits", async (ClaimsPrincipal user, CreateDepositRequest req, MttDbContext db) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();
            if (await db.CryptoDeposits.AnyAsync(c => c.Txid == req.Txid))
                return Results.Json(new { success = false, message = "Bu TXID zaten kayıtlı." }, statusCode: 409);

            db.CryptoDeposits.Add(new CryptoDeposit
            {
                Txid     = req.Txid,
                Network  = req.Network,
                Amount   = req.Amount,
                Address  = req.Address,
                Tag      = req.Tag,
                DealerId = req.DealerId > 0 ? req.DealerId : null
            });

            // Bayi bakiyesine ekle
            if (req.DealerId > 0)
            {
                var dealer = await db.Dealers.FindAsync(req.DealerId);
                if (dealer is not null)
                {
                    var usdtRate = await db.ExchangeRates.FirstOrDefaultAsync(r => r.Currency == "USDT");
                    dealer.Balance += req.Amount * (usdtRate?.BuyRate ?? 32m);
                }
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // ── LOGIN LOGS ───────────────────────────────────────────────────────
        grp.MapGet("/login-logs", async (ClaimsPrincipal user, MttDbContext db) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();
            var list = await db.LoginLogs
                .OrderByDescending(l => l.CreatedAt).Take(200)
                .Select(l => new { l.Username, ip_address = l.IpAddress, panel_type = l.PanelType, l.Success, created_at = l.CreatedAt })
                .ToListAsync();
            return Results.Ok(new { logs = list });
        });

        // ── ERROR LOGS ───────────────────────────────────────────────────────
        grp.MapGet("/error-logs", async (ClaimsPrincipal user, MttDbContext db) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();
            var list = await db.ErrorLogs
                .OrderByDescending(e => e.CreatedAt).Take(100)
                .Select(e => new { e.Type, e.Message, e.Endpoint, created_at = e.CreatedAt })
                .ToListAsync();
            return Results.Ok(new { errors = list });
        });

        // ── STATS ────────────────────────────────────────────────────────────
        grp.MapGet("/stats", async (ClaimsPrincipal user, MttDbContext db) =>
        {
            if (!IsAdmin(user)) return Results.Forbid();
            var now   = DateTime.UtcNow;
            var today = now.Date;
            var week  = today.AddDays(-7);
            var month = today.AddDays(-30);

            return Results.Ok(new
            {
                today_count     = await db.Transactions.CountAsync(t => t.CreatedAt >= today),
                week_count      = await db.Transactions.CountAsync(t => t.CreatedAt >= week),
                month_count     = await db.Transactions.CountAsync(t => t.CreatedAt >= month),
                active_dealers  = await db.Dealers.CountAsync(d => d.IsActive)
            });
        });
    }
}

// ── Request/Response DTO'ları ────────────────────────────────────────────────
record CreateOperatorRequest(string Username, string Password, string Name, string? TelegramId, bool IsAdmin);
record CreateDealerRequest(string Username, string Password, string Name, decimal Balance, string? VaultId, string? TelegramId);
record UpdateRateRequest(string Currency, decimal Buy, decimal Sell);
record CreateDepositRequest(string Txid, string Network, decimal Amount, string Address, string? Tag, int DealerId);
record BinanceTicker(string Symbol, string Price);
record ExchangeRateResponse(Dictionary<string, decimal> Rates);
