using Microsoft.EntityFrameworkCore;
using MoneyTransfer.API.Data;
using MoneyTransfer.API.Helpers;
using MoneyTransfer.API.Models;

namespace MoneyTransfer.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var grp = app.MapGroup("/api/auth");

        // POST /api/auth/login
        grp.MapPost("/login", async (LoginRequest req, MttDbContext db, IConfiguration cfg, HttpContext ctx) =>
        {
            var ip     = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var secret = Environment.GetEnvironmentVariable("MTT_JWT_SECRET")
                      ?? cfg["Jwt:Secret"] ?? "";

            var user = await db.Users
                .Include(u => u.Dealer)
                .Include(u => u.Operator)
                .FirstOrDefaultAsync(u => u.Username == req.Username && u.IsActive);

            // Giriş kaydı (başarılı veya başarısız)
            var log = new LoginLog
            {
                Username  = req.Username,
                IpAddress = ip,
                PanelType = req.PanelType,
                Success   = false
            };

            if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            {
                db.LoginLogs.Add(log);
                await db.SaveChangesAsync();
                return Results.Json(new { success = false, message = "Kullanıcı adı veya şifre hatalı." }, statusCode: 401);
            }

            // Panel tipi kontrolü
            if (req.PanelType == "dealer" && user.Role != "dealer")
                return Results.Json(new { success = false, message = "Bu hesap bayi paneline erişemez." }, statusCode: 403);

            if (req.PanelType == "operator" && user.Role == "dealer")
                return Results.Json(new { success = false, message = "Bu hesap operatör paneline erişemez." }, statusCode: 403);

            log.Success = true;
            db.LoginLogs.Add(log);
            await db.SaveChangesAsync();

            var token = JwtHelper.GenerateToken(user, secret);

            return Results.Ok(new
            {
                success = true,
                token,
                role = user.Role,
                user = new
                {
                    id       = user.Id,
                    name     = user.Name,
                    username = user.Username,
                    role     = user.Role,
                    is_admin = user.Operator?.IsAdmin ?? false
                }
            });
        })
        .WithName("Login")
        .AllowAnonymous();

        // POST /api/auth/change-password
        grp.MapPost("/change-password", async (ChangePasswordRequest req, MttDbContext db, HttpContext ctx) =>
        {
            var userId = int.Parse(ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var user   = await db.Users.FindAsync(userId);
            if (user is null) return Results.NotFound();

            if (!BCrypt.Net.BCrypt.Verify(req.OldPassword, user.PasswordHash))
                return Results.Json(new { success = false, message = "Mevcut şifre hatalı." }, statusCode: 400);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await db.SaveChangesAsync();
            return Results.Ok(new { success = true });
        })
        .RequireAuthorization();
    }
}

public record LoginRequest(string Username, string Password, string? PanelType, bool Remember = false);
public record ChangePasswordRequest(string OldPassword, string NewPassword);
