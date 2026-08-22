using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BaskentEnerji.Business.Infrastructure.ExchangeOffice.Office;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BaskentEnerji.API.HostedServices
{
    /// <summary>
    /// Günde bir kez (Türkiye saatiyle 03:00'ten sonra), bir önceki iş günü için
    /// personel anomali kontrolünü (gün sonu eksik / toplu giriş şüphesi) çalıştırır.
    /// </summary>
    public class DailyStaffAnomalyCheckBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyStaffAnomalyCheckBackgroundService> _logger;
        private DateTime? _lastRunDate;

        public DailyStaffAnomalyCheckBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<DailyStaffAnomalyCheckBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DailyStaffAnomalyCheckBackgroundService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var nowTr = DateTime.UtcNow.AddHours(3);
                    if (nowTr.Hour >= 3 && _lastRunDate != nowTr.Date)
                    {
                        var businessDate = nowTr.Date.AddDays(-1);

                        using var scope = _serviceProvider.CreateScope();
                        var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
                        await alertService.CheckStaffDailyAnomaliesAsync(businessDate);
                        await alertService.CheckVaultCountOverdueAsync();

                        _lastRunDate = nowTr.Date;
                        _logger.LogInformation("Staff daily anomaly check completed for {BusinessDate}", businessDate);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in DailyStaffAnomalyCheckBackgroundService");
                }

                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}
