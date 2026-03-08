using Microsoft.EntityFrameworkCore;
using MoneyTransferTurkey.Data.Contexts;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.API.Services
{
    public class VaultCountingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VaultCountingBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); // Check every 15 minutes

        public VaultCountingBackgroundService(IServiceProvider serviceProvider, ILogger<VaultCountingBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndUpdateVaultCounts();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in VaultCountingBackgroundService");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CheckAndUpdateVaultCounts()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MoneyTransferTurkeyDbContext>();
            
            var currentTime = DateTime.Now;
            var currentHour = currentTime.Hour;
            var currentMinute = currentTime.Minute;
            
            // Only run between 08:30 and 21:00
            if (currentHour < 8 || (currentHour == 8 && currentMinute < 30) || currentHour >= 21)
            {
                return;
            }

            // Get all active vaults
            var activeVaults = await dbContext.Vaults
                .Where(v => v.IsActive && !v.ShouldCount)
                .ToListAsync();

            foreach (var vault in activeVaults)
            {
                // Check if 3 hours have passed since last count or if there's no count today
                var lastCount = await dbContext.VaultCounts
                    .Where(vc => vc.VaultId == vault.Id)
                    .OrderByDescending(vc => vc.CountDate)
                    .FirstOrDefaultAsync();

                bool shouldSetCount = false;

                if (lastCount == null)
                {
                    // No count ever made
                    shouldSetCount = true;
                }
                else
                {
                    var timeSinceLastCount = currentTime - lastCount.CountDate;
                    
                    // Check if today has any count
                    var todayCount = await dbContext.VaultCounts
                        .Where(vc => vc.VaultId == vault.Id && 
                               vc.CountDate.Date == currentTime.Date)
                        .AnyAsync();

                    if (!todayCount)
                    {
                        // No count today
                        shouldSetCount = true;
                    }
                    else if (timeSinceLastCount >= TimeSpan.FromHours(3))
                    {
                        // 3 hours have passed since last count
                        shouldSetCount = true;
                    }
                }

                if (shouldSetCount)
                {
                    vault.ShouldCount = true;
                    vault.LastCountDate = currentTime;
                    _logger.LogInformation($"Setting ShouldCount=true for vault {vault.Name} (ID: {vault.Id})");
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}