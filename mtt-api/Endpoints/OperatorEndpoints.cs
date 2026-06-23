using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MoneyTransfer.API.Data;
using MoneyTransfer.API.Models;

namespace MoneyTransfer.API.Endpoints;

public static class OperatorEndpoints
{
    public static void MapOperatorEndpoints(this WebApplication app)
    {
        var grp = app.MapGroup("/api/operator").RequireAuthorization();

        // GET /api/operator/transactions
        grp.MapGet("/transactions", async (ClaimsPrincipal user, MttDbContext db) =>
        {
            if (user.FindFirstValue(ClaimTypes.Role) == "dealer") return Results.Forbid();

            var transactions = await db.Transactions
                .Include(t => t.Dealer)
                .Include(t => t.Operator)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id, t.CustomerName, t.Amount, t.Currency,
                    tl_amount   = t.TlAmount,
                    t.Rate, t.Status,
                    code        = t.Code,
                    dealer_name = t.Dealer.Name,
                    operator_name = t.Operator != null ? t.Operator.Name : null,
                    created_at  = t.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(new { transactions });
        });

        // GET /api/operator/chat/{txId}
        grp.MapGet("/chat/{txId:int}", async (int txId, ClaimsPrincipal user, MttDbContext db) =>
        {
            if (user.FindFirstValue(ClaimTypes.Role) == "dealer") return Results.Forbid();

            var messages = await db.ChatMessages
                .Where(m => m.TransactionId == txId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id, m.SenderType, m.Message,
                    created_at = m.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(new { messages });
        });

        // POST /api/operator/chat/{txId}
        grp.MapPost("/chat/{txId:int}", async (int txId, SendMessageRequest req,
            ClaimsPrincipal user, MttDbContext db) =>
        {
            if (user.FindFirstValue(ClaimTypes.Role) == "dealer") return Results.Forbid();

            var tx = await db.Transactions.FindAsync(txId);
            if (tx is null) return Results.NotFound();

            var msg = new ChatMessage
            {
                TransactionId = txId,
                SenderType    = "operator",
                Message       = req.Message
            };
            db.ChatMessages.Add(msg);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });

        // POST /api/operator/transaction/{txId}/{action}
        grp.MapPost("/transaction/{txId:int}/{action}", async (int txId, string action,
            ClaimsPrincipal user, MttDbContext db) =>
        {
            if (user.FindFirstValue(ClaimTypes.Role) == "dealer") return Results.Forbid();

            var tx = await db.Transactions
                .Include(t => t.Dealer)
                .FirstOrDefaultAsync(t => t.Id == txId);

            if (tx is null) return Results.NotFound();

            var operatorId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var op = await db.Operators.FirstOrDefaultAsync(o => o.UserId == operatorId);

            switch (action.ToLower())
            {
                case "accept":
                    tx.Status     = "processing";
                    tx.OperatorId = op?.Id;
                    break;

                case "complete":
                    tx.Status    = "completed";
                    tx.UpdatedAt = DateTime.UtcNow;
                    // Bayi bakiyesinden düş (ödendi)
                    tx.Dealer.Balance -= tx.TlAmount;
                    break;

                case "reject":
                case "cancel":
                    tx.Status    = "cancelled";
                    tx.UpdatedAt = DateTime.UtcNow;
                    break;

                default:
                    return Results.BadRequest(new { success = false, message = "Geçersiz işlem." });
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        });
    }
}

public record SendMessageRequest(string Message);
