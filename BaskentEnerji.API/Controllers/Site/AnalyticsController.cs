using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BaskentEnerji.Business.Infrastructure.Site;
using BaskentEnerji.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaskentEnerji.API.Controllers.Site
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpPost("track")]
        [AllowAnonymous]
        public async Task<IActionResult> TrackPageView([FromBody] TrackingDto tracking)
        {
            try
            {
                // Validate and clean the page slug
                var pageSlug = tracking.PageSlug;
                Console.WriteLine($"[Analytics] Received tracking - Raw slug: '{pageSlug}', PageId: {tracking.PageId}");
                
                if (string.IsNullOrWhiteSpace(pageSlug) || pageSlug == "undefined" || pageSlug == "null" || pageSlug == "")
                {
                    Console.WriteLine($"[Analytics] Invalid slug detected: '{pageSlug}' - defaulting to 'home'");
                    pageSlug = "home"; // Default to home if slug is invalid
                }
                
                var analytics = new Analytics
                {
                    PageId = tracking.PageId,
                    PageSlug = pageSlug,
                    SessionId = tracking.SessionId ?? Guid.NewGuid().ToString(),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    Referrer = Request.Headers["Referer"].ToString(),
                    Device = tracking.Device,
                    Browser = tracking.Browser,
                    Country = tracking.Country,
                    City = tracking.City,
                    VisitDate = DateTime.UtcNow,
                    Duration = 0,
                    IsBounce = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _analyticsService.TrackPageView(analytics);
                return Ok(new { success = true, sessionId = analytics.SessionId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] string period = "week")
        {
            try
            {
                var startDate = GetStartDate(period);
                var analytics = await _analyticsService.GetAnalytics(startDate, DateTime.UtcNow);
                
                var overview = new
                {
                    pageViews = analytics.Count(),
                    uniqueVisitors = analytics.Select(a => a.SessionId).Distinct().Count(),
                    avgSessionDuration = analytics.Any() ? analytics.Average(a => a.Duration) : 0,
                    bounceRate = analytics.Any() ? (analytics.Count(a => a.IsBounce) * 100.0 / analytics.Count()) : 0,
                    topPages = analytics.GroupBy(a => a.PageSlug)
                        .Select(g => new { page = g.Key, views = g.Count() })
                        .OrderByDescending(x => x.views)
                        .Take(10),
                    devices = analytics.GroupBy(a => a.Device ?? "Unknown")
                        .Select(g => new { device = g.Key, percentage = (g.Count() * 100.0 / analytics.Count()) }),
                    browsers = analytics.GroupBy(a => a.Browser ?? "Unknown")
                        .Select(g => new { browser = g.Key, percentage = (g.Count() * 100.0 / analytics.Count()) }),
                    referrers = analytics.Where(a => !string.IsNullOrEmpty(a.Referrer))
                        .GroupBy(a => new Uri(a.Referrer).Host)
                        .Select(g => new { source = g.Key, visits = g.Count() })
                        .OrderByDescending(x => x.visits)
                        .Take(5)
                };

                return Ok(overview);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("page-stats/{pageId}")]
        public async Task<IActionResult> GetPageStatistics(Guid pageId, [FromQuery] string period = "month")
        {
            try
            {
                var startDate = GetStartDate(period);
                var stats = await _analyticsService.GetPageStatistics(pageId, startDate, DateTime.UtcNow);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("realtime")]
        public async Task<IActionResult> GetRealtimeVisitors()
        {
            try
            {
                var activeVisitors = await _analyticsService.GetActiveVisitors(5); // Last 5 minutes
                return Ok(new
                {
                    count = activeVisitors.Count(),
                    visitors = activeVisitors
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private DateTime GetStartDate(string period)
        {
            return period.ToLower() switch
            {
                "today" => DateTime.UtcNow.Date,
                "week" => DateTime.UtcNow.AddDays(-7),
                "month" => DateTime.UtcNow.AddDays(-30),
                "year" => DateTime.UtcNow.AddYears(-1),
                _ => DateTime.UtcNow.AddDays(-7)
            };
        }
    }

    public class TrackingDto
    {
        public Guid? PageId { get; set; }
        public string PageSlug { get; set; }
        public string SessionId { get; set; }
        public string Device { get; set; }
        public string Browser { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
    }
}