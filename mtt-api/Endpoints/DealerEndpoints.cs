using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MoneyTransfer.API.Data;

namespace MoneyTransfer.API.Endpoints;

public static class DealerEndpoints
{
    public static void MapDealerEndpoints(this WebApplication app)
    {
        var grp = app.MapGroup("/api/dealer").RequireAuthorization();

        // GET /api/dealer/dashboard
        grp.MapGet("/dashboard", async (ClaimsPrincipal user, MttDbContext db) =>
        {
            var userId   = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var dealer   = await db.Dealers
                .Include(d => d.Transactions)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (dealer is null) return Results.Forbid();

            var txList = dealer.Transactions.OrderByDescending(t => t.CreatedAt).ToList();

            return Results.Ok(new
            {
                dealer_name  = dealer.Name,
                dealer_code  = dealer.DealerCode,
                balance      = dealer.Balance,
                given_tl     = txList.Where(t => t.Status == "completed").Sum(t => t.TlAmount),
                usdt         = txList.Where(t => t.Status == "completed" && t.Currency == "USDT").Sum(t => t.Amount),
                rub          = txList.Where(t => t.Status == "completed" && t.Currency == "RUB").Sum(t => t.Amount),
                transactions = txList.Select(t => new
                {
                    t.Id, t.CustomerName, t.Amount, t.Currency,
                    tl_amount  = t.TlAmount,
                    t.Rate, t.Status,
                    code       = t.Code,
                    created_at = t.CreatedAt
                })
            });
        });

        // GET /api/dealer/rates  — mevcut kurları döndür
        grp.MapGet("/rates", async (MttDbContext db) =>
        {
            var rates = await db.ExchangeRates.ToListAsync();
            return Results.Ok(rates.Select(r => new
            {
                r.Currency, r.BuyRate, r.SellRate,
                updated_at = r.UpdatedAt
            }));
        });
    }
}
