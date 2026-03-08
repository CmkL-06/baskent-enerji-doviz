using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BaskentEnerji.Business.Services.Coin;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.Site;
using BaskentEnerji.Entity.Entities.User;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BaskentEnerji.API
{
    public class HostService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CoinPriceService _coinPriceService;
        private readonly ILogger<HostService> _logger;

        public HostService(IServiceProvider serviceProvider, CoinPriceService coinPriceService, ILogger<HostService> logger)
        {
            _serviceProvider = serviceProvider;
            _coinPriceService = coinPriceService;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<BaskentEnerjiDbContext>();

                    // Check and initialize languages
                    try
                    {
                        if (await dbContext.Languages.CountAsync(cancellationToken) < 1)
                {
                    Language nLang = new Language
                    {
                        Id = Guid.NewGuid(),
                        IsDefault = true,
                        IsEnabled = true,
                        LanguageCode = "en",
                        LanguageName = "English",
                        FlagUri = "https://flagsapi.com/GB/flat/32.png",

                    };
                            await dbContext.Languages.AddAsync(nLang, cancellationToken);
                            await dbContext.SaveChangesAsync(cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not initialize languages. This might be normal on first run.");
                    }

                    // Check and initialize users
                    try
                    {
                        if (await dbContext.Users.CountAsync(cancellationToken) < 1)
                {
                    string username = "admin";
                    string password = "123456asD!";
                    string mail = "admin@quanta.com";
                    string hashedPassword;
                    using (var sha256 = SHA256.Create())
                    {
                        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                        var builder = new StringBuilder();
                        foreach (var b in bytes)
                        {
                            builder.Append(b.ToString("x2"));
                        }
                        hashedPassword = builder.ToString();
                    }

                    User nUser = new User
                    {
                        Firstname = "Super",
                        Lastname = "Mario",
                        Gender = Entity.Gender.Male,
                        LanguageCode = "en",
                        IsEmailVerified = true,
                        Mail = mail,
                        Rank = Entity.Rank.Owner,
                        Username = username,
                        Password = hashedPassword
                    };
                            await dbContext.Users.AddAsync(nUser, cancellationToken);
                            await dbContext.SaveChangesAsync(cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not initialize users. This might be normal on first run.");
                    }

                    // Check and initialize pages
                    try
                    {
                        if (await dbContext.Pages.CountAsync(cancellationToken) < 1)
                {
                    Guid pId = Guid.NewGuid();

                    // HTML içeriğini JSON olarak sar
                    var props = new
                    {
                        content = "<h1><span style=\"background-color: #e03e2d; color: #ffffff;\"><strong>&nbsp;<span style=\"color: #ecf0f1;\">HELLO</span> <span style=\"background-color: #b96ad9;\"><span style=\"color: #000000;\">&nbsp;WORLD!</span>&nbsp;</span></strong></span></h1>"
                    };

                    string propsJson = JsonSerializer.Serialize(props);

                    var componentId = Guid.NewGuid();

                    var languageId = dbContext.Languages.FirstOrDefault()?.Id ?? Guid.NewGuid(); // varsa kullan, yoksa boş geçme

                    var nPage = new Entity.Entities.Site.Page.Page
                    {
                        IsHomePage = true,
                        Slug = "home",
                        Title = "Home",
                        LanguageCode = "en",
                        Id = pId,
                        LanguageId = languageId,
                        Status = Entity.Entities.Site.Page.PageStatus.Published
                    };

                    var nComponent = new Entity.Entities.Site.Page.BuilderComponent
                    {
                        Id = componentId,
                        PageId = pId,
                        Type = "TextFieldComponent",
                        Name = "Welcome Text",
                        PropsJson = propsJson,
                        Order = 0
                    };

                            nPage.Components.Add(nComponent);

                            await dbContext.Pages.AddAsync(nPage, cancellationToken);
                            await dbContext.SaveChangesAsync(cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not initialize pages. This might be normal on first run.");
                    }

                    // Initialize coin tracking
                    try
                    {
                        var dbCoins = await dbContext.Coins.ToListAsync(cancellationToken);

                        foreach (var coin in dbCoins)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            string coinName = coin.Name.ToLower();
                            coinName = coinName.Replace("ı", "i").Replace("İ", "i").Replace("I", "i");
                            await _coinPriceService.StartTrackingCoin(coinName + "usdt", coin.Exchange ?? "binance");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not initialize coin tracking. This is not critical.");
                    }
                }

                _logger.LogInformation("HostService initialization completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during HostService initialization. The application will continue but some features may not work.");
                // Don't throw - let the application continue
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Perform any cleanup or resource release if necessary
            return Task.CompletedTask;
        }
    }
}
