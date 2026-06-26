using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BaskentEnerji.Business.Services.ExchangeOffice.AutoRate;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BaskentEnerji.API.HostedServices
{
    /// <summary>
    /// Background service for automatic exchange rate updates
    /// Runs as a hosted service without requiring Hangfire
    /// </summary>
    public class AutoRateUpdateBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoRateUpdateBackgroundService> _logger;
        private Timer? _timer;

        public AutoRateUpdateBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<AutoRateUpdateBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoRateUpdateBackgroundService is starting.");

            // Wait for 30 seconds after startup before first check
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndUpdateRatesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AutoRateUpdateBackgroundService");
                }

                // Check every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task CheckAndUpdateRatesAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BaskentEnerjiDbContext>();

            try
            {
                var settings = await context.Set<ExchangeSettings>().FirstOrDefaultAsync(cancellationToken);

                // If no settings or auto-update disabled, skip
                if (settings == null || !settings.IsAutoUpdateEnabled)
                {
                    return;
                }

                var now = DateTime.Now;
                var currentHour = now.Hour;
                var currentDay = now.DayOfWeek;

                // Check working hours
                if (currentHour < settings.StartHour || currentHour >= settings.EndHour)
                {
                    return;
                }

                // Check working days
                if (!string.IsNullOrEmpty(settings.WorkDays))
                {
                    List<string>? workDays = null;
                    try
                    {
                        workDays = System.Text.Json.JsonSerializer.Deserialize<List<string>>(settings.WorkDays);
                    }
                    catch
                    {
                        // Legacy comma-separated format: "1,2,3,4,5,6,7" — treat as all days active
                        workDays = null;
                    }
                    var todayName = currentDay.ToString().ToLower();

                    if (workDays != null && !workDays.Contains(todayName))
                    {
                        return;
                    }
                }

                // Check if enough time has passed since last update
                if (settings.LastAutoUpdate.HasValue)
                {
                    var minutesSinceLastUpdate = (DateTime.UtcNow - settings.LastAutoUpdate.Value).TotalMinutes;
                    if (minutesSinceLastUpdate < settings.UpdateIntervalMinutes)
                    {
                        return;
                    }
                }

                // Execute the update
                _logger.LogInformation("Starting automatic rate update...");

                var updateService = scope.ServiceProvider.GetRequiredService<AutoRateUpdateService>();
                var request = new AutoUpdateRequestDto
                {
                    OfficeId = null, // Update all offices
                    TriggeredBy = "BACKGROUND_SERVICE"
                };

                var result = await updateService.UpdateRatesAsync(request);

                _logger.LogInformation(
                    "Automatic rate update completed. Updated: {Updated}, Pending: {Pending}, Skipped: {Skipped}",
                    result.RatesUpdated,
                    result.RatesPendingApproval,
                    result.RatesSkipped
                );

                // Update last auto-update time
                settings.LastAutoUpdate = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing automatic rate update");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AutoRateUpdateBackgroundService is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}
