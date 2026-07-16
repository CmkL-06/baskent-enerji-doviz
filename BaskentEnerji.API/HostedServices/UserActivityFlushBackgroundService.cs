using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BaskentEnerji.API.Infrastructure;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.User;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BaskentEnerji.API.HostedServices
{
    /// <summary>
    /// Periodically flushes the in-memory UserActivityTracker buffer into
    /// Users.LastActivityDate, instead of writing to the DB on every request.
    /// </summary>
    public class UserActivityFlushBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly UserActivityTracker _tracker;
        private readonly ILogger<UserActivityFlushBackgroundService> _logger;

        public UserActivityFlushBackgroundService(
            IServiceProvider serviceProvider,
            UserActivityTracker tracker,
            ILogger<UserActivityFlushBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _tracker = tracker;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("UserActivityFlushBackgroundService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

                try
                {
                    await FlushAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in UserActivityFlushBackgroundService");
                }
            }
        }

        private async Task FlushAsync(CancellationToken cancellationToken)
        {
            var snapshot = _tracker.DrainSnapshot();
            if (snapshot.Count == 0)
                return;

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BaskentEnerjiDbContext>();

            foreach (var (userId, lastActivity) in snapshot)
            {
                await context.Users
                    .Where(u => u.Id == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastActivityDate, lastActivity), cancellationToken);

                context.UserActivityHistories.Add(new UserActivityHistory
                {
                    UserId = userId,
                    PingDate = lastActivity
                });
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
