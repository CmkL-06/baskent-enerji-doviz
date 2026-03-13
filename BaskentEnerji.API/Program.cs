using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using BaskentEnerji.API;
using BaskentEnerji.API.Services;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Extensions;
using BaskentEnerji.Business.Hubs;
using BaskentEnerji.Business.Infrastructure.Blog.Article;
using BaskentEnerji.Business.Infrastructure.Blog.Category;
using BaskentEnerji.Business.Infrastructure.Cache;
using BaskentEnerji.Business.Infrastructure.Coin;
using BaskentEnerji.Business.Infrastructure.Email;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Expense;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Party;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Blockchain;
using BaskentEnerji.Business.Services.ExchangeOffice.AutoRate;
using BaskentEnerji.API.HostedServices;
using BaskentEnerji.Business.Infrastructure.Site.General;
using BaskentEnerji.Business.Infrastructure.Site.Language;
using BaskentEnerji.Business.Infrastructure.Site.Menu;
using BaskentEnerji.Business.Infrastructure.Site.Page;
using BaskentEnerji.Business.Infrastructure.Site.Slider;
using BaskentEnerji.Business.Infrastructure.Site.Tag;
using BaskentEnerji.Business.Infrastructure.Site.Theme;
using BaskentEnerji.Business.Infrastructure.Site;
using BaskentEnerji.Business.Infrastructure.User;
using BaskentEnerji.Business.Services.Blog;
using BaskentEnerji.Business.Services.Blog.Article;
using BaskentEnerji.Business.Services.Blog.Category;
using BaskentEnerji.Business.Services.Cache;
using BaskentEnerji.Business.Services.Coin;
using BaskentEnerji.Business.Services.Email;
using BaskentEnerji.Business.Services.ExchangeOffice;
using BaskentEnerji.Business.Services.ExchangeOffice.Expense;
using BaskentEnerji.Business.Services.ExchangeOffice.Office;
using BaskentEnerji.Business.Services.ExchangeOffice.Party;
using BaskentEnerji.Business.Services.ExchangeOffice.Blockchain;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Business.Services.Site.General;
using BaskentEnerji.Business.Services.Site.Language;
using BaskentEnerji.Business.Services.Site.Menu;
using BaskentEnerji.Business.Services.Site.Page;
using BaskentEnerji.Business.Services.Site.Slider;
using BaskentEnerji.Business.Services.Site.Tag;
using BaskentEnerji.Business.Services.Site.Theme;
using BaskentEnerji.Business.Services.Site;
using BaskentEnerji.Business.Services.User;
using BaskentEnerji.Data.Contexts;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
//builder.Logging.AddDebug();
//builder.Logging.SetMinimumLevel(LogLevel.Debug);


builder.Services.AddDbContext<BaskentEnerjiDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SQL"),
        sqlServerOptionsAction: sqlOptions =>
        {
            // Retry strategy removed - it conflicts with user transactions
            // If you need retry logic, implement it at the service layer
            sqlOptions.CommandTimeout(60);

            // Configure migrations history table to use mtturkey_exchange schema
            sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "mtturkey_exchange");
        });
    // Performance optimizations
    options.EnableServiceProviderCaching();
    // DO NOT use NoTracking as default - it breaks write operations
    // Instead, use AsNoTracking() explicitly on read-only queries
});
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Don't ignore null values - we need them for floatingContactJson
        // options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 32;
        options.JsonSerializerOptions.WriteIndented = true;
    });


builder.Services.AddLogging();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName); // Use full type name to avoid conflicts
});



builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Don't ignore null values - we need them for floatingContactJson
    // options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.WriteIndented = true;
});




// JWT Authentication Configuration
var key = Encoding.ASCII.GetBytes(builder.Configuration["JwtSecretKey"]);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtIssuer"],
        ValidAudience = builder.Configuration["JwtAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

    // Token invalidation on password change
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<BaskentEnerji.Data.Contexts.BaskentEnerjiDbContext>();
            var userIdClaim = context.Principal?.FindFirst("UserId");

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                var user = await dbContext.Users.FindAsync(userId);

                if (user != null && user.LastPasswordChangeDate.HasValue)
                {
                    // Get token issued at time (iat claim)
                    var iatClaim = context.Principal?.FindFirst("iat");
                    if (iatClaim != null && long.TryParse(iatClaim.Value, out var iat))
                    {
                        var tokenIssuedAt = DateTimeOffset.FromUnixTimeSeconds(iat).UtcDateTime;

                        // If password was changed after token was issued, reject the token
                        if (user.LastPasswordChangeDate.Value > tokenIssuedAt)
                        {
                            context.Fail("Token invalidated: password has been changed");
                        }
                    }
                }
            }
        }
    };
});

//var firebaseConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "firebaseConfig.json");
//var googleCredential = GoogleCredential.FromFile(firebaseConfigPath);

//FirebaseApp.Create(new AppOptions
//{
 //   Credential = googleCredential,
//});


builder.Services.AddHostedService<HostService>();
builder.Services.AddHostedService<VaultCountingBackgroundService>();
builder.Services.AddHostedService<AutoRateUpdateBackgroundService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<ISettingsServiceCommand, SettingsServiceCommand>();
builder.Services.AddScoped<ISettingsServiceQuery, SettingsServiceQuery>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IUserServiceCommand, UserServiceCommand>();
builder.Services.AddScoped<IUserServiceQuery, UserServiceQuery>();
builder.Services.AddScoped<IBlog_CategoryServiceCommand, Blog_CategoryServiceCommand>();
builder.Services.AddScoped<IBlog_CategoryServiceQuery, Blog_CategoryServiceQuery>();
builder.Services.AddScoped<IBlog_ArticleServiceCommand, Blog_ArticleServiceCommand>();
builder.Services.AddScoped<IBlog_ArticleServiceQuery, Blog_ArticleServiceQuery>();
builder.Services.AddScoped<ITagServiceCommand, TagServiceCommand>();
builder.Services.AddScoped<ITagServiceQuery, TagServiceQuery>();
builder.Services.AddScoped<IMenuServiceCommand, MenuServiceCommand>();
builder.Services.AddScoped<IMenuServiceQuery, MenuServiceQuery>();
builder.Services.AddScoped<ISliderServiceCommand, SliderServiceCommand>();
builder.Services.AddScoped<ISliderServiceQuery, SliderServiceQuery>();
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<ISeoService, SeoService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<ILanguageServiceQuery, LanguageServiceQuery>();
builder.Services.AddScoped<ILanguageServiceCommand, LanguageServiceCommand>();
//builder.Services.AddScoped<ICoinPriceService, CoinPriceService>();
builder.Services.AddSingleton<CoinPriceService>();
builder.Services.AddScoped<ICoinServiceCommand, CoinServiceCommand>();
builder.Services.AddScoped<ICoinServiceQuery, CoinServiceQuery>();
builder.Services.AddScoped<IThemeServiceCommand, ThemeServiceCommand>();
builder.Services.AddScoped<IThemeServiceQuery, ThemeServiceQuery>();

builder.Services.AddScoped<IExchangeServiceCommand, ExchangeServiceCommand>();
builder.Services.AddScoped<IExchangeServiceQuery, ExchangeServiceQuery>();
builder.Services.AddScoped<IVaultService, VaultService>();
builder.Services.AddScoped<IVaultSnapshotService, VaultSnapshotService>();
builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
builder.Services.AddScoped<IExchangeTransactionService, ExchangeTransactionService>();
builder.Services.AddScoped<IUserOfficeService, UserOfficeService>();
builder.Services.AddScoped<IExchangeValidationService, ExchangeValidationService>();
builder.Services.AddScoped<IExchangeReportingService, ExchangeReportingService>();
builder.Services.AddScoped<IZReportService, ZReportService>();
builder.Services.AddScoped<IOfficeServiceCommand, OfficeServiceCommand>();

// Party Account Services
builder.Services.AddScoped<IPartyService, PartyService>();
builder.Services.AddScoped<IGhostPartyService, GhostPartyService>();
builder.Services.AddScoped<IPartyAccountService, PartyAccountService>();
builder.Services.AddScoped<IPartyCreditService, PartyCreditService>();
builder.Services.AddScoped<IPartyReportingService, PartyReportingService>();
builder.Services.AddScoped<PartyTransactionIntegration>();
builder.Services.AddScoped<ICacheClearService, CacheClearService>();
builder.Services.AddScoped<ZReportService>();
// Expense Services
builder.Services.AddScoped<IExpenseDefinitionService, ExpenseDefinitionService>();
builder.Services.AddScoped<IExpensePaymentService, ExpensePaymentService>();

// Binance API Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<ITRC20Service>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var logger = provider.GetRequiredService<ILogger<BinanceService>>();
    return new BinanceService(httpClientFactory, logger);
});

// Auto Rate Services
// Register TcmbProvider as IExternalRateProvider
builder.Services.AddScoped<IExternalRateProvider>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return new TcmbProvider(httpClientFactory.CreateClient());
});

// Register BinanceProvider as IExternalRateProvider
builder.Services.AddScoped<IExternalRateProvider>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return new BinanceProvider(httpClientFactory.CreateClient());
});

// DovizComProvider - 4 different instances
builder.Services.AddScoped<IExternalRateProvider>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return new DovizComProvider(httpClientFactory.CreateClient(), "harem", "Harem");
});
builder.Services.AddScoped<IExternalRateProvider>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return new DovizComProvider(httpClientFactory.CreateClient(), "ziraat", "Ziraat");
});
builder.Services.AddScoped<IExternalRateProvider>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return new DovizComProvider(httpClientFactory.CreateClient(), "ptt", "PTT");
});
builder.Services.AddScoped<IExternalRateProvider>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return new DovizComProvider(httpClientFactory.CreateClient(), "kapali-carsi", "Kapalıçarşı");
});

builder.Services.AddScoped<RateCalculationService>();
builder.Services.AddScoped<AnomalyDetectionService>();
builder.Services.AddScoped<AutoRateUpdateService>();
builder.Services.AddScoped<AutoRateUpdateJob>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", builder =>
    {
        builder.WithOrigins(
            "http://localhost:3000",            
            "http://localhost:3001",            
            "http://localhost:3002",            
            "http://localhost:3003",            
            "http://localhost:3004",            
            "http://localhost:3005",            
            "http://localhost:3006",            
            "http://localhost:3007",            
            "http://localhost:3008",            
            "http://localhost:3009",            
            "http://localhost:3010",            
            "http://localhost:3011",            
            "http://localhost:3012",            
            "http://localhost:5175",            
            "http://localhost:5174",            
            "http://localhost:5173",
             "https://localhost:5173",    
            "http://localhost:5174",            
            "http://localhost:5175",            
            "http://localhost:5179",
            "http://localhost:5093",
            "https://localhost:7197",
            "http://127.0.0.1:5093",
            "http://127.0.0.1:5173",
            "http://baskentenerji.com",
            "https://baskentenerji.com",
            "http://old.baskentenerji.com",
            "https://old.baskentenerji.com",
               "https://moneytransferturkey.com",
               "http://moneytransferturkey.com"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});



var app = builder.Build();
app.Logger.LogInformation("Application has started.");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Güvenlik başlıkları (canlıda)
if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        await next();
    });
    app.UseHttpsRedirection();
}

app.UseCors("AllowVueApp");

app.UseAuthentication();

app.UseAuthorization();

// Sağlık kontrolü (canlı/deploy sonrası izleme; kimlik doğrulama gerekmez)
app.MapGet("/health/live", () => Results.Ok(new
{
    status = "alive",
    timestamp = DateTime.UtcNow
})).AllowAnonymous();

app.MapGet("/health/ready", async (IServiceProvider serviceProvider) =>
{
    try
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BaskentEnerjiDbContext>();
        var canConnect = await db.Database.CanConnectAsync();

        if (!canConnect)
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Ok(new
        {
            status = "ready",
            db = "ok",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            status = "not-ready",
            db = "error",
            error = ex.Message,
            timestamp = DateTime.UtcNow
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    endpoints = new[] { "/health/live", "/health/ready", "/api/v1/Diagnostics/ping" },
    timestamp = DateTime.UtcNow
})).AllowAnonymous();

app.MapControllers();
app.MapHub<CoinPriceHub>("/coinPriceHub");

app.Run();
