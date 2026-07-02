using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.ExchangeOffice.AutoRate
{
    /// <summary>
    /// Background job for automatic rate updates
    /// Can be scheduled using Hangfire, Windows Task Scheduler, or any other scheduler
    /// </summary>
    public class AutoRateUpdateJob
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly AutoRateUpdateService _updateService;
        private readonly ILogger<AutoRateUpdateJob> _logger;

        public AutoRateUpdateJob(
            BaskentEnerjiDbContext context,
            AutoRateUpdateService updateService,
            ILogger<AutoRateUpdateJob> logger)
        {
            _context = context;
            _updateService = updateService;
            _logger = logger;
        }

        /// <summary>
        /// Main execution method for the scheduled job
        /// </summary>
        public async Task ExecuteAsync()
        {
            try
            {
                _logger.LogInformation("Auto rate update job started at {Time}", DateTime.UtcNow);

                // Check if auto-update is enabled
                var settings = await _context.Set<Entity.Entities.ExchangeOffice.Currency.ExchangeSettings>()
                    .OrderBy(s => s.CreatedDate)
                    .FirstOrDefaultAsync();

                if (settings == null || !settings.IsAutoUpdateEnabled)
                {
                    _logger.LogInformation("Auto-update is disabled, skipping job execution");
                    return;
                }

                // Check if we're within working hours
                var now = DateTime.Now;
                var currentHour = now.Hour;
                var currentDay = now.DayOfWeek;

                if (currentHour < settings.StartHour || currentHour >= settings.EndHour)
                {
                    _logger.LogInformation("Outside working hours ({StartHour}-{EndHour}), skipping job execution",
                        settings.StartHour, settings.EndHour);
                    return;
                }

                // Check if today is a work day (if WorkDays is configured)
                if (!string.IsNullOrEmpty(settings.WorkDays))
                {
                    var workDays = System.Text.Json.JsonSerializer.Deserialize<List<string>>(settings.WorkDays);
                    var todayName = currentDay.ToString().ToLower();

                    if (workDays != null && !workDays.Contains(todayName))
                    {
                        _logger.LogInformation("Today ({Day}) is not a work day, skipping job execution", currentDay);
                        return;
                    }
                }

                // Execute the update
                var request = new AutoUpdateRequestDto
                {
                    OfficeId = null, // Update all offices
                    TriggeredBy = "SCHEDULED_JOB"
                };

                var result = await _updateService.UpdateRatesAsync(request);

                _logger.LogInformation(
                    "Auto rate update job completed. Updated: {Updated}, Pending: {Pending}, Skipped: {Skipped}",
                    result.RatesUpdated,
                    result.RatesPendingApproval,
                    result.RatesSkipped
                );

                // Update last auto-update time
                settings.LastAutoUpdate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing auto rate update job");
                throw;
            }
        }

        /// <summary>
        /// Simple method signature for Hangfire
        /// Usage in Startup.cs or Program.cs:
        /// RecurringJob.AddOrUpdate&lt;AutoRateUpdateJob&gt;(
        ///     x => x.ExecuteAsync(),
        ///     Cron.MinuteInterval(30)
        /// );
        /// </summary>
    }
}
