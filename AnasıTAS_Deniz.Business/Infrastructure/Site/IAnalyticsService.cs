using AnasıTAS_Deniz.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Infrastructure.Site
{
    public interface IAnalyticsService
    {
        Task TrackPageView(Analytics analytics);
        Task<List<Analytics>> GetAnalytics(DateTime startDate, DateTime endDate);
        Task<object> GetPageStatistics(Guid pageId, DateTime startDate, DateTime endDate);
        Task<List<Analytics>> GetActiveVisitors(int minutes);
        Task UpdateSessionDuration(string sessionId, int duration);
    }
}