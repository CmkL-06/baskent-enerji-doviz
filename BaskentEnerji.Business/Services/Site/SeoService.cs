using BaskentEnerji.Business.Infrastructure.Site;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.Site;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BaskentEnerji.Business.Services.Site
{
    public class SeoService : ISeoService
    {
        private readonly BaskentEnerjiDbContext _dbContext;

        public SeoService(BaskentEnerjiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<SeoSettings> GetGlobalSettings()
        {
            var settings = await _dbContext.SeoSettings
                .FirstOrDefaultAsync(s => s.PageId == null);
            
            if (settings == null)
            {
                // Create default global settings if not exists
                settings = new SeoSettings
                {
                    PageId = null,
                    MetaTitle = "Default Site Title",
                    MetaDescription = "Default site description",
                    IncludeInSitemap = true,
                    NoIndex = false,
                    NoFollow = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.SeoSettings.Add(settings);
                await _dbContext.SaveChangesAsync();
            }
            
            return settings;
        }

        public async Task UpdateGlobalSettings(SeoSettings settings)
        {
            var existing = await _dbContext.SeoSettings
                .FirstOrDefaultAsync(s => s.PageId == null);
            
            if (existing != null)
            {
                existing.MetaTitle = settings.MetaTitle;
                existing.MetaDescription = settings.MetaDescription;
                existing.MetaKeywords = settings.MetaKeywords;
                existing.MetaAuthor = settings.MetaAuthor;
                existing.OgTitle = settings.OgTitle;
                existing.OgDescription = settings.OgDescription;
                existing.OgImage = settings.OgImage;
                existing.OgType = settings.OgType;
                existing.TwitterCard = settings.TwitterCard;
                existing.TwitterSite = settings.TwitterSite;
                existing.TwitterCreator = settings.TwitterCreator;
                existing.SchemaMarkup = settings.SchemaMarkup;
                existing.UpdatedAt = DateTime.UtcNow;
                
                _dbContext.SeoSettings.Update(existing);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                settings.PageId = null;
                settings.CreatedAt = DateTime.UtcNow;
                settings.UpdatedAt = DateTime.UtcNow;
                _dbContext.SeoSettings.Add(settings);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<SeoSettings> GetPageSettings(Guid pageId)
        {
            var settings = await _dbContext.SeoSettings
                .FirstOrDefaultAsync(s => s.PageId == pageId);
            
            if (settings == null)
            {
                // Return global settings as default
                return await GetGlobalSettings();
            }
            
            return settings;
        }

        public async Task UpdatePageSettings(Guid pageId, SeoSettings settings)
        {
            var existing = await _dbContext.SeoSettings
                .FirstOrDefaultAsync(s => s.PageId == pageId);
            
            if (existing != null)
            {
                existing.MetaTitle = settings.MetaTitle;
                existing.MetaDescription = settings.MetaDescription;
                existing.MetaKeywords = settings.MetaKeywords;
                existing.MetaAuthor = settings.MetaAuthor;
                existing.OgTitle = settings.OgTitle;
                existing.OgDescription = settings.OgDescription;
                existing.OgImage = settings.OgImage;
                existing.OgType = settings.OgType;
                existing.TwitterCard = settings.TwitterCard;
                existing.TwitterSite = settings.TwitterSite;
                existing.TwitterCreator = settings.TwitterCreator;
                existing.CanonicalUrl = settings.CanonicalUrl;
                existing.NoIndex = settings.NoIndex;
                existing.NoFollow = settings.NoFollow;
                existing.IncludeInSitemap = settings.IncludeInSitemap;
                existing.SitemapPriority = settings.SitemapPriority;
                existing.SitemapChangeFrequency = settings.SitemapChangeFrequency;
                existing.UpdatedAt = DateTime.UtcNow;
                
                _dbContext.SeoSettings.Update(existing);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                settings.PageId = pageId;
                settings.CreatedAt = DateTime.UtcNow;
                settings.UpdatedAt = DateTime.UtcNow;
                _dbContext.SeoSettings.Add(settings);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<string> GetRobotsTxt()
        {
            var siteSettings = await _dbContext.Site_Settings
                .FirstOrDefaultAsync();
            
            // You can store robots.txt in Site_Settings or create a new field
            // For now, returning a default value
            return @"User-agent: *
Allow: /
Disallow: /admin/
Disallow: /api/
Sitemap: /api/v1/seo/sitemap.xml";
        }

        public async Task UpdateRobotsTxt(string content)
        {
            // Store in Site_Settings or a dedicated table
            var siteSettings = await _dbContext.Site_Settings
                .FirstOrDefaultAsync();
            if (siteSettings != null)
            {
                // You may need to add a RobotsTxt field to Site_Settings entity
                // siteSettings.RobotsTxt = content;
                _dbContext.Site_Settings.Update(siteSettings);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task GenerateSitemap()
        {
            // This is handled by the controller
            await Task.CompletedTask;
        }
    }
}