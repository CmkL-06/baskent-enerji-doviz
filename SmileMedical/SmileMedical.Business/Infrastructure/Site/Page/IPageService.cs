using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PageEntity = SmileMedical.Entity.Entities.Site.Page.Page;
using SmileMedical.Entity.Entities.Site.Page;
using SmileMedical.Entity.Modals.RequestModals.Site.Page;

namespace SmileMedical.Business.Infrastructure.Site.Page
{
    public interface IPageService
    {
        // Read operations
        Task<List<PageEntity>> GetPagesAsync(string? languageCode = null, PageStatus? status = null);
        Task<PageEntity?> GetPageByIdAsync(Guid id);
        Task<PageEntity?> GetPageBySlugAsync(string slug, string? languageCode = null);
        Task<PageEntity?> GetHomePageAsync(string? languageCode = null);
        
        // Write operations
        Task<PageEntity> CreatePageAsync(CreatePageRequest request);
        Task<bool> UpdatePageAsync(Guid id, UpdatePageRequest request);
        Task<bool> DeletePageAsync(Guid id);
        
        // Page actions
        Task<bool> PublishPageAsync(Guid id);
        Task<bool> UnpublishPageAsync(Guid id);
        Task<PageEntity?> DuplicatePageAsync(Guid id, DuplicatePageRequest? request = null);
        Task<bool> SetAsHomePageAsync(Guid id);
    }
}
