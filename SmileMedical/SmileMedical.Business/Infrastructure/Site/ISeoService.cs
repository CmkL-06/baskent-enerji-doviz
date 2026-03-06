using SmileMedical.Entity.Entities.Site;
using System;
using System.Threading.Tasks;

namespace SmileMedical.Business.Infrastructure.Site
{
    public interface ISeoService
    {
        Task<SeoSettings> GetGlobalSettings();
        Task UpdateGlobalSettings(SeoSettings settings);
        Task<SeoSettings> GetPageSettings(Guid pageId);
        Task UpdatePageSettings(Guid pageId, SeoSettings settings);
        Task<string> GetRobotsTxt();
        Task UpdateRobotsTxt(string content);
        Task GenerateSitemap();
    }
}