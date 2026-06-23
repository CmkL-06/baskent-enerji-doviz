using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MoneyTransfer.API.Data;
using MoneyTransfer.API.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// ── Bağlantı dizesi: Machine ortam değişkeninden ────────────────────────────
var connStr = Environment.GetEnvironmentVariable("MTT_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("MttDb")
    ?? throw new InvalidOperationException("MTT_DB_CONNECTION ortam değişkeni tanımlı değil.");

var jwtSecret = Environment.GetEnvironmentVariable("MTT_JWT_SECRET")
    ?? builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("MTT_JWT_SECRET ortam değişkeni tanımlı değil.");

// ── Servisler ───────────────────────────────────────────────────────────────
builder.Services.AddDbContext<MttDbContext>(opt =>
    opt.UseSqlServer(connStr));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer   = false,
            ValidateAudience = false,
            ClockSkew        = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(opt =>
    opt.AddPolicy("MttFrontend", p =>
        p.WithOrigins(
            "https://tg.moneytransferturkey.com",
            "http://localhost:3000"          // geliştirme ortamı
        )
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

var app = builder.Build();

// ── Middleware ─────────────────────────────────────────────────────────────
app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    ctx.Response.StatusCode  = 500;
    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsJsonAsync(new { error = ex?.Message, type = ex?.GetType().Name });
}));
app.UseCors("MttFrontend");
app.UseAuthentication();
app.UseAuthorization();

// ── Route grupları ─────────────────────────────────────────────────────────
app.MapAuthEndpoints();
app.MapDealerEndpoints();
app.MapOperatorEndpoints();
app.MapAdminEndpoints();

// ── Veritabanı şema oluştur ────────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MttDbContext>();
    db.Database.EnsureCreated();
    app.Logger.LogInformation("Veritabani hazir: mtturkey_mtt");
}
catch (Exception ex)
{
    // Startup hatası dosyaya yaz — IIS logda görünmeyebilir
    var logPath = Path.Combine(AppContext.BaseDirectory, "startup-error.txt");
    File.WriteAllText(logPath,
        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] STARTUP HATASI\n" +
        $"Type: {ex.GetType().FullName}\n" +
        $"Message: {ex.Message}\n" +
        $"InnerException: {ex.InnerException?.Message}\n" +
        $"StackTrace:\n{ex.StackTrace}");
    // Uygulamayı durdurma — loglama için devam et
    app.Logger.LogCritical(ex, "Veritabani baslatma hatasi!");
}

app.Run();
