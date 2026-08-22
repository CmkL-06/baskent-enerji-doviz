using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BaskentEnerji.Data.Contexts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BaskentEnerji.API.HostedServices
{
    // Dış sağlayıcı kur cache tablosu (ExternalRateCaches) her fetch'de append-only büyür —
    // günde binlerce satır. Tarihsel değeri yok (transfer hesabı ExchangeRateHistories'i kullanır),
    // sadece son değerleri sorgulanır. Günde bir kez 7 gün ötesindeki kayıtları siler.
    public class ExternalRateCacheCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExternalRateCacheCleanupBackgroundService> _logger;
        private DateTime? _lastRunDate;
        private const int RetentionDays = 7;

        public ExternalRateCacheCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ExternalRateCacheCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExternalRateCacheCleanup starting; retention={Days} days", RetentionDays);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var nowTr = DateTime.UtcNow.AddHours(3);
                    // TR saatiyle 03:30–04:00 arasında bir kez çalış (anomali kontrol jobundan sonra).
                    if (nowTr.Hour == 3 && nowTr.Minute >= 30 && _lastRunDate != nowTr.Date)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<BaskentEnerjiDbContext>();

                        var threshold = DateTime.UtcNow.AddDays(-RetentionDays);
                        var deleted = await db.Database.ExecuteSqlRawAsync(
                            "DELETE FROM ExternalRateCaches WHERE FetchedAt < {0}", threshold);

                        _lastRunDate = nowTr.Date;
                        _logger.LogInformation("ExternalRateCacheCleanup: {Deleted} row(s) deleted (older than {Threshold:o})", deleted, threshold);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ExternalRateCacheCleanupBackgroundService");
                }

                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}
