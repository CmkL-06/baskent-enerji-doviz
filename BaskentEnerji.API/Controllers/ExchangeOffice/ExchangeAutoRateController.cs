using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Services.ExchangeOffice.AutoRate;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.ExchangeOffice.Currency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BaskentEnerji.API.Controllers.ExchangeOffice
{
    [Route("api/v1/exchange/auto-rate")]
    [ApiController]
    [Authorize]
    public class ExchangeAutoRateController : ControllerBase
    {
        private readonly BaskentEnerjiDbContext _context;
        private readonly AutoRateUpdateService _updateService;
        private readonly ILogger<ExchangeAutoRateController> _logger;

        public ExchangeAutoRateController(
            BaskentEnerjiDbContext context,
            AutoRateUpdateService updateService,
            ILogger<ExchangeAutoRateController> logger)
        {
            _context = context;
            _updateService = updateService;
            _logger = logger;
        }

        /// <summary>
        /// Get current settings
        /// </summary>
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var settings = await _context.Set<ExchangeSettings>()
                    .AsNoTracking()
                    .OrderBy(s => s.CreatedDate)
                    .FirstOrDefaultAsync();

                if (settings == null)
                {
                    // Create default settings
                    settings = new ExchangeSettings();
                    await _context.Set<ExchangeSettings>().AddAsync(settings);
                    await _context.SaveChangesAsync();
                }

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting auto-rate settings");
                return StatusCode(500, new { error = "İç sunucu hatası" });
            }
        }

        /// <summary>
        /// Update settings
        /// </summary>
        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] SaveSettingsRequestDto request)
        {
            try
            {
                var settings = await _context.Set<ExchangeSettings>()
                    .OrderBy(s => s.CreatedDate)
                    .FirstOrDefaultAsync();

                if (settings == null)
                {
                    settings = new ExchangeSettings();
                    await _context.Set<ExchangeSettings>().AddAsync(settings);
                }

                // Update properties
                settings.IsAutoUpdateEnabled = request.IsAutoUpdateEnabled;
                settings.UpdateIntervalMinutes = request.UpdateIntervalMinutes;
                settings.StartHour = request.StartHour;
                settings.EndHour = request.EndHour;
                settings.WorkDays = request.WorkDays != null ? JsonSerializer.Serialize(request.WorkDays) : null;

                settings.TryBasedMarginPercent = request.TryBasedMarginPercent;
                settings.CrossFiatMarginPercent = request.CrossFiatMarginPercent;
                settings.CryptoMarginPercent = request.CryptoMarginPercent;

                settings.UseTcmb = request.UseTcmb;
                settings.UseDovizCom = request.UseDovizCom;
                settings.UseBinance = request.UseBinance;

                settings.RateSelectionStrategy = request.RateSelectionStrategy;

                settings.MaxPriceChangePercent = request.MaxPriceChangePercent;
                settings.RequireApprovalAboveThreshold = request.RequireApprovalAboveThreshold;

                settings.NotificationEmails = request.NotificationEmails != null ? JsonSerializer.Serialize(request.NotificationEmails) : null;
                settings.SendMobileNotifications = request.SendMobileNotifications;

                await _context.SaveChangesAsync();

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating auto-rate settings");
                return StatusCode(500, new { error = "İç sunucu hatası" });
            }
        }

        /// <summary>
        /// Test - Only fetch external rates without updating
        /// </summary>
        [HttpPost("test-fetch")]
        public async Task<IActionResult> TestFetchRates()
        {
            try
            {
                var settings = await _context.Set<ExchangeSettings>()
                    .AsNoTracking()
                    .OrderBy(s => s.CreatedDate)
                    .FirstOrDefaultAsync() ?? new ExchangeSettings();

                // Call the TestFetchOnly method we'll add to the service
                var result = await _updateService.TestFetchOnlyAsync(settings);

                // Save to cache for display purposes
                if (result.ExternalRates != null && result.ExternalRates.Any())
                {
                    var cacheEntries = result.ExternalRates.Select(r => new ExternalRateCache
                    {
                        Source = r.Source,
                        CurrencyCode = r.CurrencyCode,
                        TargetCurrencyCode = r.TargetCurrencyCode,
                        BuyRate = r.BuyRate,
                        SellRate = r.SellRate,
                        SpreadPercent = r.SpreadPercent,
                        FetchedAt = r.FetchedAt,
                        IsValid = r.IsValid,
                        ErrorMessage = r.ErrorMessage
                    }).ToList();

                    // Clear old cache entries (keep for 24 hours instead of 1 hour)
                    var oldEntries = await _context.Set<ExternalRateCache>()
                        .Where(e => e.FetchedAt < DateTime.UtcNow.AddHours(-24))
                        .ToListAsync();
                    _context.Set<ExternalRateCache>().RemoveRange(oldEntries);

                    await _context.Set<ExternalRateCache>().AddRangeAsync(cacheEntries);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = result.Success,
                    message = result.Success ? "Test fetch completed - rates displayed but NOT updated" : "Test fetch failed",
                    externalRates = result.ExternalRates,
                    summary = result.Summary ?? $"Fetched {result.ExternalRates?.Count ?? 0} rates from external sources",
                    sources = result.ExternalRates?.GroupBy(r => r.Source).Select(g => new
                    {
                        source = g.Key,
                        count = g.Count(),
                        currencies = g.Select(r => r.CurrencyCode).Distinct()
                    }),
                    errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test fetch");
                return StatusCode(500, new { error = "İç sunucu hatası" });
            }
        }

        /// <summary>
        /// Trigger manual auto-update
        /// </summary>
        [HttpPost("update")]
        public async Task<IActionResult> TriggerUpdate([FromBody] AutoUpdateRequestDto request)
        {
            try
            {
                // Check for test mode
                if (request?.TriggeredBy == "TEST")
                {
                    return await TestFetchRates();
                }

                var result = await _updateService.UpdateRatesAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering auto-update");
                return StatusCode(500, new { error = "İç sunucu hatası" });
            }
        }

        /// <summary>
        /// Get pending approvals
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingApprovals([FromQuery] Guid? officeId = null)
        {
            try
            {
                var query = _context.Set<PendingRateApproval>()
                    .Include(p => p.Office)
                    .Include(p => p.SourceCurrency)
                    .Include(p => p.TargetCurrency)
                    .Where(p => p.Status == "PENDING");

                if (officeId.HasValue)
                {
                    query = query.Where(p => p.OfficeId == officeId.Value);
                }

                var pending = await query
                    .OrderByDescending(p => p.CreatedDate)
                    .ToListAsync();

                var result = pending.Select(p => new PendingApprovalDto
                {
                    Id = p.Id,
                    OfficeName = p.Office?.OfficeName,
                    CurrencyPair = $"{p.SourceCurrency?.CurrencyCode}/{p.TargetCurrency?.CurrencyCode}",
                    CurrentBuyRate = p.CurrentBuyRate,
                    CurrentSellRate = p.CurrentSellRate,
                    ProposedBuyRate = p.ProposedBuyRate,
                    ProposedSellRate = p.ProposedSellRate,
                    ChangePercent = p.ChangePercent,
                    Reason = p.Reason,
                    CreatedAt = p.CreatedDate,
                    SourceData = string.IsNullOrEmpty(p.SourceData)
                        ? null
                        : JsonSerializer.Deserialize<List<ExternalRateDto>>(p.SourceData)
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending approvals");
                return StatusCode(500, new { error = "İç sunucu hatası" });
            }
        }

        /// <summary>
        /// Approve or reject pending rate
        /// </summary>
        [HttpPost("pending/{id}/action")]
        public async Task<IActionResult> ApprovalAction(Guid id, [FromBody] ApprovalActionDto action)
        {
            try
            {
                var pending = await _context.Set<PendingRateApproval>()
                    .Where(p => p.Id == id)
                    .FirstOrDefaultAsync();

                if (pending == null)
                {
                    return NotFound(new { error = "Pending approval not found" });
                }

                pending.Status = action.IsApproved ? "APPROVED" : "REJECTED";
                pending.ApprovedBy = action.ApprovedBy;
                pending.ApprovedAt = DateTime.UtcNow;
                pending.ApprovalNotes = action.Notes;

                if (action.IsApproved)
                {
                    // Update the rate (no AsNoTracking - we need to update it)
                    var rate = await _context.Set<ExchangeRate>()
                        .Where(r =>
                            r.OfficeId == pending.OfficeId &&
                            r.SourceCurrencyId == pending.SourceCurrencyId &&
                            r.TargetCurrencyId == pending.TargetCurrencyId &&
                            r.IsActive)
                        .FirstOrDefaultAsync();

                    if (rate != null)
                    {
                        // Create history
                        var history = new ExchangeRateHistory
                        {
                            OfficeId = pending.OfficeId,
                            SourceCurrencyId = pending.SourceCurrencyId,
                            TargetCurrencyId = pending.TargetCurrencyId,
                            OldBuyRate = rate.BuyRate,
                            OldSellRate = rate.SellRate,
                            NewBuyRate = pending.ProposedBuyRate,
                            NewSellRate = pending.ProposedSellRate,
                            ChangePercent = pending.ChangePercent,
                            UpdateSource = "MANUAL_APPROVAL",
                            UserId = action.ApprovedBy,
                            IsApproved = true
                        };

                        await _context.Set<ExchangeRateHistory>().AddAsync(history);

                        // Update rate
                        rate.BuyRate = pending.ProposedBuyRate;
                        rate.SellRate = pending.ProposedSellRate;
                        rate.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, status = pending.Status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing approval action");
                return StatusCode(500, new { error = "İç sunucu hatası" });
            }
        }

        /// <summary>
        /// Get rate history
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] Guid? officeId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.Set<ExchangeRateHistory>()
                    .Include(h => h.Office)
                    .Include(h => h.SourceCurrency)
                    .Include(h => h.TargetCurrency)
                    .AsQueryable();

                if (officeId.HasValue)
                {
                    query = query.Where(h => h.OfficeId == officeId.Value);
                }

                var total = await query.CountAsync();

                var history = await query
                    .OrderByDescending(h => h.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = history.Select(h => new RateHistoryDto
                {
                    Id = h.Id,
                    OfficeName = h.Office?.OfficeName,
                    CurrencyPair = $"{h.SourceCurrency?.CurrencyCode}/{h.TargetCurrency?.CurrencyCode}",
                    OldBuyRate = h.OldBuyRate,
                    OldSellRate = h.OldSellRate,
                    NewBuyRate = h.NewBuyRate,
                    NewSellRate = h.NewSellRate,
                    ChangePercent = h.ChangePercent,
                    UpdateSource = h.UpdateSource,
                    DataSources = string.IsNullOrEmpty(h.DataSources)
                        ? null
                        : JsonSerializer.Deserialize<List<string>>(h.DataSources),
                    UserName = h.UserName,
                    CreatedAt = h.CreatedDate
                }).ToList();

                return Ok(new { total, page, pageSize, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rate history");
                return StatusCode(500, new { error = "İç sunucu hatası" });
            }
        }

        /// <summary>
        /// Get external rates from cache
        /// </summary>
        [HttpGet("external-rates")]
        public async Task<IActionResult> GetExternalRates([FromQuery] int minutes = 60)
        {
            try
            {
                var since = DateTime.UtcNow.AddMinutes(-minutes);

                var rates = await _context.Set<ExternalRateCache>()
                    .Where(r => r.FetchedAt >= since && r.IsValid)
                    .OrderByDescending(r => r.FetchedAt)
                    .ToListAsync();

                // Group by currency and source
                var grouped = rates
                    .GroupBy(r => new { r.CurrencyCode, r.Source })
                    .Select(g => g.OrderByDescending(r => r.FetchedAt).First())
                    .ToList();

                return Ok(grouped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting external rates");
                return StatusCode(500, new { error = "İç sunucu hatası" });
            }
        }

        /// <summary>
        /// Get external data sources status
        /// </summary>
        [HttpGet("sources")]
        public async Task<IActionResult> GetSources()
        {
            try
            {
                var sources = await _context.Set<ExternalDataSource>()
                    .OrderBy(s => s.Priority)
                    .ToListAsync();

                // If no sources exist, seed them
                if (!sources.Any())
                {
                    await SeedExternalDataSources();
                    sources = await _context.Set<ExternalDataSource>()
                        .OrderBy(s => s.Priority)
                        .ToListAsync();
                }

                return Ok(sources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sources");
                return StatusCode(500, new { error = "İç sunucu hatası" });
            }
        }

        /// <summary>
        /// Seed default external data sources
        /// </summary>
        [HttpPost("sources/seed")]
        public async Task<IActionResult> SeedDataSources()
        {
            try
            {
                await SeedExternalDataSources();
                var sources = await _context.Set<ExternalDataSource>()
                    .OrderBy(s => s.Priority)
                    .ToListAsync();
                return Ok(sources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding sources");
                return StatusCode(500, new { error = "İç sunucu hatası" });
            }
        }

        private async Task SeedExternalDataSources()
        {
            var existingSources = await _context.Set<ExternalDataSource>().AnyAsync();
            if (existingSources) return;

            var sources = new List<ExternalDataSource>
            {
                new ExternalDataSource
                {
                    SourceName = "TCMB",
                    SourceType = "API",
                    SourceKey = "TCMB",
                    BaseUrl = "https://www.tcmb.gov.tr/kurlar/today.xml",
                    IsEnabled = true,
                    Priority = 1
                },
                new ExternalDataSource
                {
                    SourceName = "Harem",
                    SourceType = "WEB_SCRAPE",
                    SourceKey = "DOVIZ_COM_HAREM",
                    BaseUrl = "https://kur.doviz.com/harem",
                    IsEnabled = true,
                    Priority = 1
                },
                new ExternalDataSource
                {
                    SourceName = "Kapalıçarşı",
                    SourceType = "WEB_SCRAPE",
                    SourceKey = "DOVIZ_COM_KAPALICARSI",
                    BaseUrl = "https://kur.doviz.com/kapali-carsi",
                    IsEnabled = true,
                    Priority = 2
                },
                new ExternalDataSource
                {
                    SourceName = "Ziraat",
                    SourceType = "WEB_SCRAPE",
                    SourceKey = "DOVIZ_COM_ZIRAAT",
                    BaseUrl = "https://kur.doviz.com/ziraat",
                    IsEnabled = true,
                    Priority = 3
                },
                new ExternalDataSource
                {
                    SourceName = "PTT",
                    SourceType = "WEB_SCRAPE",
                    SourceKey = "DOVIZ_COM_PTT",
                    BaseUrl = "https://kur.doviz.com/ptt",
                    IsEnabled = true,
                    Priority = 3
                },
                new ExternalDataSource
                {
                    SourceName = "Binance",
                    SourceType = "API",
                    SourceKey = "BINANCE",
                    BaseUrl = "https://api.binance.com/api/v3",
                    IsEnabled = true,
                    Priority = 2
                }
            };

            await _context.Set<ExternalDataSource>().AddRangeAsync(sources);
            await _context.SaveChangesAsync();
        }
    }
}
