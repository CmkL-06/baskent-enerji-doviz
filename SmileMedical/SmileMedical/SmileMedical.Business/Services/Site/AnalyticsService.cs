using SmileMedical.Business.Infrastructure.Site;
using SmileMedical.Data.Contexts;
using SmileMedical.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SmileMedical.Business.Services.Site
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly SmileMedicalDbContext _dbContext;

        public AnalyticsService(SmileMedicalDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task TrackPageView(Analytics analytics)
        {
            _dbContext.Analytics.Add(analytics);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<Analytics>> GetAnalytics(DateTime startDate, DateTime endDate)
        {
            return await _dbContext.Analytics
                .Where(a => a.VisitDate >= startDate && a.VisitDate <= endDate)
                .ToListAsync();
        }

        public async Task<object> GetPageStatistics(Guid pageId, DateTime startDate, DateTime endDate)
        {
            var analyticsList = await _dbContext.Analytics
                .Where(a => a.PageId == pageId && a.VisitDate >= startDate && a.VisitDate <= endDate)
                .ToListAsync();

            return new
            {
                totalViews = analyticsList.Count,
                uniqueVisitors = analyticsList.Select(a => a.SessionId).Distinct().Count(),
                avgDuration = analyticsList.Any() ? analyticsList.Average(a => a.Duration) : 0,
                bounceRate = analyticsList.Any() ? 
                    (analyticsList.Count(a => a.IsBounce) * 100.0 / analyticsList.Count) : 0,
                devices = analyticsList.GroupBy(a => a.Device ?? "Unknown")
                    .Select(g => new { device = g.Key, count = g.Count() }),
                browsers = analyticsList.GroupBy(a => a.Browser ?? "Unknown")
                    .Select(g => new { browser = g.Key, count = g.Count() }),
                dailyViews = analyticsList.GroupBy(a => a.VisitDate.Date)
                    .Select(g => new { date = g.Key, views = g.Count() })
                    .OrderBy(x => x.date)
            };
        }

        public async Task<List<Analytics>> GetActiveVisitors(int minutes)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-minutes);
            return await _dbContext.Analytics
                .Where(a => a.VisitDate >= cutoffTime)
                .ToListAsync();
        }

        public async Task UpdateSessionDuration(string sessionId, int duration)
        {
            var session = await _dbContext.Analytics
                .FirstOrDefaultAsync(a => a.SessionId == sessionId);

            if (session != null)
            {
                session.Duration = duration;
                _dbContext.Analytics.Update(session);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}