using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BaskentEnerji.Business.Infrastructure.Site;
using BaskentEnerji.Business.Infrastructure.Site.Page;
using BaskentEnerji.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BaskentEnerji.API.Controllers.Site
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class SeoController : ControllerBase
    {
        private readonly ISeoService _seoService;
        private readonly IPageService _pageService;

        public SeoController(ISeoService seoService, IPageService pageService)
        {
            _seoService = seoService;
            _pageService = pageService;
        }

        [HttpGet("settings/global")]
        public async Task<IActionResult> GetGlobalSeoSettings()
        {
            try
            {
                var settings = await _seoService.GetGlobalSettings();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("settings/global")]
        public async Task<IActionResult> UpdateGlobalSeoSettings([FromBody] SeoSettingsDto dto)
        {
            try
            {
                var settings = new SeoSettings
                {
                    PageId = null, // Global settings
                    MetaTitle = dto.MetaTitle ?? "",
                    MetaDescription = dto.MetaDescription ?? "",
                    MetaKeywords = dto.MetaKeywords ?? "",
                    MetaAuthor = dto.MetaAuthor ?? "",
                    OgTitle = dto.OgTitle ?? "",
                    OgDescription = dto.OgDescription ?? "",
                    OgImage = dto.OgImage ?? "",
                    OgType = dto.OgType ?? "website",
                    TwitterCard = dto.TwitterCard ?? "summary",
                    TwitterSite = dto.TwitterSite ?? "",
                    TwitterCreator = dto.TwitterCreator ?? "",
                    SchemaMarkup = dto.SchemaMarkup ?? "",
                    CanonicalUrl = "",
                    NoIndex = false,
                    NoFollow = false,
                    IncludeInSitemap = true,
                    SitemapPriority = "1.0",
                    SitemapChangeFrequency = "daily",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _seoService.UpdateGlobalSettings(settings);
                return Ok(new { success = true, message = "Global SEO settings updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("settings/page/{pageId}")]
        public async Task<IActionResult> GetPageSeoSettings(Guid pageId)
        {
            try
            {
                var settings = await _seoService.GetPageSettings(pageId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("settings/page/{pageId}")]
        public async Task<IActionResult> UpdatePageSeoSettings(Guid pageId, [FromBody] SeoSettingsDto dto)
        {
            try
            {
                var settings = new SeoSettings
                {
                    PageId = pageId,
                    MetaTitle = dto.MetaTitle ?? "",
                    MetaDescription = dto.MetaDescription ?? "",
                    MetaKeywords = dto.MetaKeywords ?? "",
                    MetaAuthor = dto.MetaAuthor ?? "",
                    OgTitle = dto.OgTitle ?? "",
                    OgDescription = dto.OgDescription ?? "",
                    OgImage = dto.OgImage ?? "",
                    OgType = dto.OgType ?? "website",
                    TwitterCard = dto.TwitterCard ?? "summary",
                    TwitterSite = dto.TwitterSite ?? "",
                    TwitterCreator = dto.TwitterCreator ?? "",
                    CanonicalUrl = dto.CanonicalUrl ?? "",
                    SchemaMarkup = dto.SchemaMarkup ?? "",
                    NoIndex = dto.NoIndex ?? false,
                    NoFollow = dto.NoFollow ?? false,
                    IncludeInSitemap = dto.IncludeInSitemap ?? true,
                    SitemapPriority = dto.SitemapPriority ?? "0.5",
                    SitemapChangeFrequency = dto.SitemapChangeFrequency ?? "weekly",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _seoService.UpdatePageSettings(pageId, settings);
                return Ok(new { success = true, message = "Page SEO settings updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("sitemap.xml")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateSitemap()
        {
            try
            {
                var pages = await _pageService.GetPagesAsync();
                var baseUrl = $"{Request.Scheme}://{Request.Host}";

                XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
                var sitemap = new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    new XElement(ns + "urlset",
                        pages.Where(p => p.Status == Entity.Entities.Site.Page.PageStatus.Published).Select(page =>
                            new XElement(ns + "url",
                                new XElement(ns + "loc", $"{baseUrl}/{page.Slug}"),
                                new XElement(ns + "lastmod", page.CreatedDate.ToString("yyyy-MM-dd")),
                                new XElement(ns + "changefreq", "weekly"),
                                new XElement(ns + "priority", page.IsHomePage ? "1.0" : "0.8")
                            )
                        )
                    )
                );

                return Content(sitemap.ToString(), "application/xml", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("robots.txt")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRobotsTxt()
        {
            try
            {
                var robotsTxt = await _seoService.GetRobotsTxt();
                if (string.IsNullOrEmpty(robotsTxt))
                {
                    robotsTxt = @"User-agent: *
Allow: /
Disallow: /admin/
Disallow: /api/
Sitemap: /api/v1/seo/sitemap.xml";
                }
                return Content(robotsTxt, "text/plain", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("robots.txt")]
        public async Task<IActionResult> UpdateRobotsTxt([FromBody] RobotsTxtDto dto)
        {
            try
            {
                await _seoService.UpdateRobotsTxt(dto.Content);
                return Ok(new { success = true, message = "robots.txt updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("analyze/{pageId}")]
        public async Task<IActionResult> AnalyzePageSeo(Guid pageId)
        {
            try
            {
                var page = await _pageService.GetPageByIdAsync(pageId);
                var settings = await _seoService.GetPageSettings(pageId);
                
                var score = CalculateSeoScore(page, settings);
                var recommendations = GetSeoRecommendations(page, settings);

                return Ok(new
                {
                    score,
                    recommendations,
                    analysis = new
                    {
                        hasTitle = !string.IsNullOrEmpty(settings?.MetaTitle ?? page?.Title),
                        hasDescription = !string.IsNullOrEmpty(settings?.MetaDescription ?? page?.Description),
                        hasKeywords = !string.IsNullOrEmpty(settings?.MetaKeywords),
                        hasOgImage = !string.IsNullOrEmpty(settings?.OgImage),
                        hasCanonicalUrl = !string.IsNullOrEmpty(settings?.CanonicalUrl)
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private int CalculateSeoScore(Entity.Entities.Site.Page.Page page, SeoSettings settings)
        {
            int score = 0;
            
            // Title (20 points)
            if (!string.IsNullOrEmpty(settings?.MetaTitle ?? page?.Title))
            {
                score += 10;
                if ((settings?.MetaTitle ?? page?.Title).Length >= 30 && (settings?.MetaTitle ?? page?.Title).Length <= 60)
                    score += 10;
            }
            
            // Description (20 points)
            if (!string.IsNullOrEmpty(settings?.MetaDescription ?? page?.Description))
            {
                score += 10;
                if ((settings?.MetaDescription ?? page?.Description).Length >= 120 && (settings?.MetaDescription ?? page?.Description).Length <= 160)
                    score += 10;
            }
            
            // Keywords (10 points)
            if (!string.IsNullOrEmpty(settings?.MetaKeywords))
                score += 10;
            
            // OG Tags (20 points)
            if (!string.IsNullOrEmpty(settings?.OgTitle))
                score += 10;
            if (!string.IsNullOrEmpty(settings?.OgImage))
                score += 10;
            
            // Technical SEO (30 points)
            if (!string.IsNullOrEmpty(settings?.CanonicalUrl))
                score += 10;
            if (settings?.IncludeInSitemap == true)
                score += 10;
            if (!string.IsNullOrEmpty(settings?.SchemaMarkup))
                score += 10;
            
            return Math.Min(score, 100);
        }

        private List<string> GetSeoRecommendations(Entity.Entities.Site.Page.Page page, SeoSettings settings)
        {
            var recommendations = new List<string>();
            
            if (string.IsNullOrEmpty(settings?.MetaTitle ?? page?.Title))
                recommendations.Add("Add a meta title for better search visibility");
            else if ((settings?.MetaTitle ?? page?.Title).Length < 30)
                recommendations.Add("Meta title is too short. Aim for 30-60 characters");
            else if ((settings?.MetaTitle ?? page?.Title).Length > 60)
                recommendations.Add("Meta title is too long. Keep it under 60 characters");
            
            if (string.IsNullOrEmpty(settings?.MetaDescription ?? page?.Description))
                recommendations.Add("Add a meta description to improve click-through rates");
            else if ((settings?.MetaDescription ?? page?.Description).Length < 120)
                recommendations.Add("Meta description is too short. Aim for 120-160 characters");
            else if ((settings?.MetaDescription ?? page?.Description).Length > 160)
                recommendations.Add("Meta description is too long. Keep it under 160 characters");
            
            if (string.IsNullOrEmpty(settings?.OgImage))
                recommendations.Add("Add an Open Graph image for better social media sharing");
            
            if (string.IsNullOrEmpty(settings?.CanonicalUrl))
                recommendations.Add("Set a canonical URL to avoid duplicate content issues");
            
            return recommendations;
        }
    }

    public class SeoSettingsDto
    {
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        public string? MetaAuthor { get; set; }
        public string? OgTitle { get; set; }
        public string? OgDescription { get; set; }
        public string? OgImage { get; set; }
        public string? OgType { get; set; }
        public string? TwitterCard { get; set; }
        public string? TwitterSite { get; set; }
        public string? TwitterCreator { get; set; }
        public string? SchemaMarkup { get; set; }
        public string? CanonicalUrl { get; set; }
        public bool? NoIndex { get; set; }
        public bool? NoFollow { get; set; }
        public bool? IncludeInSitemap { get; set; }
        public string? SitemapPriority { get; set; }
        public string? SitemapChangeFrequency { get; set; }
    }

    public class RobotsTxtDto
    {
        public string Content { get; set; }
    }
}