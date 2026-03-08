using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Infrastructure.Site.Page;
using BaskentEnerji.Data.Contexts;
using PageEntity = BaskentEnerji.Entity.Entities.Site.Page.Page;
using BaskentEnerji.Entity.Entities.Site.Page;
using BaskentEnerji.Entity.Modals.RequestModals.Site.Page;

namespace BaskentEnerji.Business.Services.Site.Page
{
    public class PageService : IPageService
    {
        private readonly BaskentEnerjiDbContext _dbContext;
        private readonly IMapper _mapper;

        public PageService(BaskentEnerjiDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        // Read operations
        public async Task<List<PageEntity>> GetPagesAsync(string? languageCode = null, PageStatus? status = null)
        {
            var query = _dbContext.Pages
                .AsNoTracking() // Prevent tracking to avoid circular references
                .AsQueryable();

            if (!string.IsNullOrEmpty(languageCode))
                query = query.Where(p => p.LanguageCode == languageCode);

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            // Get pages without components to avoid circular reference
            var pages = await query.OrderBy(p => p.Title).ToListAsync();
            
            // Load components separately if needed
            foreach (var page in pages)
            {
                var components = await _dbContext.BuilderComponents
                    .AsNoTracking()
                    .Where(c => c.PageId == page.Id && c.ParentId == null)
                    .ToListAsync();
                    
                // Load children for each component
                foreach (var component in components)
                {
                    await LoadComponentChildren(component);
                    component.Page = null; // Break circular reference
                }
                
                page.Components = components;
            }

            return pages;
        }
        
        private async Task LoadComponentChildren(BuilderComponent component)
        {
            var children = await _dbContext.BuilderComponents
                .AsNoTracking()
                .Where(c => c.ParentId == component.Id)
                .ToListAsync();
            
            foreach (var child in children)
            {
                child.Page = null; // Break circular reference
                child.Parent = null; // Break parent reference
                await LoadComponentChildren(child); // Recursive load
            }
            
            component.Children = children;
        }

        public async Task<PageEntity?> GetPageByIdAsync(Guid id)
        {
            var page = await _dbContext.Pages
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (page == null) return null;
            
            // Load components separately
            var components = await _dbContext.BuilderComponents
                .AsNoTracking()
                .Where(c => c.PageId == page.Id && c.ParentId == null)
                .ToListAsync();
                
            foreach (var component in components)
            {
                await LoadComponentChildren(component);
                component.Page = null; // Break circular reference
            }
            
            page.Components = components;
            return page;
        }

        public async Task<PageEntity?> GetPageBySlugAsync(string slug, string? languageCode = null)
        {
            var query = _dbContext.Pages
                .AsNoTracking()
                .Where(p => p.Slug == slug);

            if (!string.IsNullOrEmpty(languageCode))
                query = query.Where(p => p.LanguageCode == languageCode);

            var page = await query.FirstOrDefaultAsync();
            
            if (page == null) return null;
            
            // Load components separately
            var components = await _dbContext.BuilderComponents
                .AsNoTracking()
                .Where(c => c.PageId == page.Id && c.ParentId == null)
                .ToListAsync();
                
            foreach (var component in components)
            {
                await LoadComponentChildren(component);
                component.Page = null; // Break circular reference
            }
            
            page.Components = components;
            return page;
        }

        public async Task<PageEntity?> GetHomePageAsync(string? languageCode = null)
        {
            var query = _dbContext.Pages
                .AsNoTracking()
                .Where(p => p.IsHomePage);

            if (!string.IsNullOrEmpty(languageCode))
                query = query.Where(p => p.LanguageCode == languageCode);

            var page = await query.FirstOrDefaultAsync();
            
            if (page == null) return null;
            
            // Load components separately
            var components = await _dbContext.BuilderComponents
                .AsNoTracking()
                .Where(c => c.PageId == page.Id && c.ParentId == null)
                .ToListAsync();
                
            foreach (var component in components)
            {
                await LoadComponentChildren(component);
                component.Page = null; // Break circular reference
            }
            
            page.Components = components;
            return page;
        }

        // Write operations
        public async Task<PageEntity> CreatePageAsync(CreatePageRequest request)
        {
            // Check if slug already exists
            var existingPage = await _dbContext.Pages
                .AnyAsync(p => p.Slug == request.Slug && p.LanguageCode == request.LanguageCode);
            
            if (existingPage)
                throw new InvalidOperationException($"A page with slug '{request.Slug}' already exists for language '{request.LanguageCode}'");

            // If setting as home page, unset other home pages for this language
            if (request.IsHomePage)
            {
                var currentHomePages = await _dbContext.Pages
                    .Where(p => p.IsHomePage && p.LanguageCode == request.LanguageCode)
                    .ToListAsync();
                
                foreach (var hp in currentHomePages)
                {
                    hp.IsHomePage = false;
                }
            }

            var page = new PageEntity
            {
                Id = Guid.NewGuid(),
                Slug = request.Slug,
                Title = request.Title,
                Description = request.Description,
                Keywords = request.Keywords,
                LanguageCode = request.LanguageCode,
                IsHomePage = request.IsHomePage,
                Status = request.Status,
                CustomCSS = request.CustomCSS,
                CustomJS = request.CustomJS,
                OgImage = request.OgImage,
                OgTitle = request.OgTitle,
                OgDescription = request.OgDescription,
                FloatingContactJson = request.FloatingContact != null 
                    ? JsonSerializer.Serialize(request.FloatingContact) 
                    : null,
                BackgroundColor = request.BackgroundColor,
                CreatedDate = DateTime.UtcNow
            };

            // Add components if provided
            if (request.Components != null && request.Components.Any())
            {
                page.Components = MapComponentDtos(request.Components, page.Id, null);
            }

            _dbContext.Pages.Add(page);
            await _dbContext.SaveChangesAsync();

            return page;
        }

        public async Task<bool> UpdatePageAsync(Guid id, UpdatePageRequest request)
        {
            try
            {
                // First check if page exists
                var pageExists = await _dbContext.Pages.AnyAsync(p => p.Id == id);
                if (!pageExists)
                    return false;

                // Check slug uniqueness if updating
                if (!string.IsNullOrEmpty(request.Slug))
                {
                    var slugExists = await _dbContext.Pages
                        .AnyAsync(p => p.Id != id && p.Slug == request.Slug && 
                                 p.LanguageCode == (request.LanguageCode ?? "en"));
                    
                    if (slugExists)
                        throw new InvalidOperationException($"A page with slug '{request.Slug}' already exists");
                }

                // Handle IsHomePage - unset other home pages if needed
                if (request.IsHomePage.HasValue && request.IsHomePage.Value)
                {
                    await _dbContext.Database.ExecuteSqlRawAsync(
                        "UPDATE Pages SET IsHomePage = 0 WHERE Id != {0} AND LanguageCode = {1} AND IsHomePage = 1",
                        id, request.LanguageCode ?? "en");
                }

                // Update page properties using raw SQL to avoid tracking
                var sql = @"
                    UPDATE Pages SET 
                        Slug = COALESCE(@p0, Slug),
                        Title = COALESCE(@p1, Title),
                        Description = COALESCE(@p2, Description),
                        Keywords = COALESCE(@p3, Keywords),
                        LanguageCode = COALESCE(@p4, LanguageCode),
                        Status = COALESCE(@p5, Status),
                        CustomCSS = @p6,
                        CustomJS = @p7,
                        OgImage = @p8,
                        OgTitle = @p9,
                        OgDescription = @p10,
                        IsHomePage = COALESCE(@p11, IsHomePage),
                        FloatingContactJson = @p12,
                        BackgroundColor = @p13
                    WHERE Id = @p14";

                await _dbContext.Database.ExecuteSqlRawAsync(sql,
                    string.IsNullOrEmpty(request.Slug) ? DBNull.Value : request.Slug,
                    string.IsNullOrEmpty(request.Title) ? DBNull.Value : request.Title,
                    request.Description,
                    request.Keywords,
                    string.IsNullOrEmpty(request.LanguageCode) ? DBNull.Value : request.LanguageCode,
                    request.Status.HasValue ? (object)request.Status.Value : DBNull.Value,
                    request.CustomCSS,
                    request.CustomJS,
                    request.OgImage,
                    request.OgTitle,
                    request.OgDescription,
                    request.IsHomePage.HasValue ? (object)request.IsHomePage.Value : DBNull.Value,
                    request.FloatingContact != null 
                        ? JsonSerializer.Serialize(request.FloatingContact) 
                        : (object)DBNull.Value,
                    request.BackgroundColor,
                    id);

                // Handle components update
                if (request.Components != null)
                {
                    // Delete all existing components for this page
                    await _dbContext.Database.ExecuteSqlRawAsync(
                        "DELETE FROM BuilderComponents WHERE PageId = {0}", id);
                    
                    // Add new components if any
                    if (request.Components.Any())
                    {
                        var newComponents = MapComponentDtos(request.Components, id, null);
                        _dbContext.BuilderComponents.AddRange(newComponents);
                        await _dbContext.SaveChangesAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"Error in UpdatePageAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> DeletePageAsync(Guid id)
        {
            var page = await _dbContext.Pages
                .Include(p => p.Components)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (page == null)
                return false;

            // First delete related analytics records
            var analytics = await _dbContext.Analytics
                .Where(a => a.PageId == id)
                .ToListAsync();

            if (analytics.Any())
            {
                _dbContext.Analytics.RemoveRange(analytics);
            }

            // Then delete the page
            _dbContext.Pages.Remove(page);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // Page actions
        public async Task<bool> PublishPageAsync(Guid id)
        {
            var page = await _dbContext.Pages.FindAsync(id);
            if (page == null)
                return false;

            page.Status = PageStatus.Published;
            page.PublishedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnpublishPageAsync(Guid id)
        {
            var page = await _dbContext.Pages.FindAsync(id);
            if (page == null)
                return false;

            page.Status = PageStatus.Draft;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<PageEntity?> DuplicatePageAsync(Guid id, DuplicatePageRequest? request = null)
        {
            var originalPage = await _dbContext.Pages
                .FirstOrDefaultAsync(p => p.Id == id);

            if (originalPage == null)
                return null;

            // Load all components for this page separately to avoid duplicate includes
            var allComponents = await _dbContext.BuilderComponents
                .Where(c => c.PageId == id)
                .ToListAsync();

            // Build component tree - only get root components (ParentId == null)
            originalPage.Components = allComponents.Where(c => c.ParentId == null).ToList();

            // Recursively assign children to each component
            foreach (var component in originalPage.Components)
            {
                AssignChildren(component, allComponents);
            }

            var newPage = new PageEntity
            {
                Id = Guid.NewGuid(),
                Slug = request?.NewSlug ?? $"{originalPage.Slug}-copy",
                Title = request?.NewTitle ?? $"{originalPage.Title} (Copy)",
                Description = originalPage.Description,
                Keywords = originalPage.Keywords,
                LanguageCode = request?.LanguageCode ?? originalPage.LanguageCode,
                IsHomePage = false, // Never duplicate as home page
                Status = PageStatus.Draft, // Always start as draft
                CustomCSS = originalPage.CustomCSS,
                CustomJS = originalPage.CustomJS,
                OgImage = originalPage.OgImage,
                OgTitle = originalPage.OgTitle,
                OgDescription = originalPage.OgDescription,
                FloatingContactJson = originalPage.FloatingContactJson, // Copy floating contact settings
                CreatedDate = DateTime.UtcNow
            };

            // Deep clone components
            if (originalPage.Components.Any())
            {
                newPage.Components = CloneComponents(originalPage.Components, newPage.Id, null);
            }

            _dbContext.Pages.Add(newPage);
            await _dbContext.SaveChangesAsync();

            return newPage;
        }

        public async Task<bool> SetAsHomePageAsync(Guid id)
        {
            var page = await _dbContext.Pages.FindAsync(id);
            if (page == null)
                return false;

            // Unset other home pages for this language
            var currentHomePages = await _dbContext.Pages
                .Where(p => p.Id != id && p.IsHomePage && p.LanguageCode == page.LanguageCode)
                .ToListAsync();
            
            foreach (var hp in currentHomePages)
            {
                hp.IsHomePage = false;
            }

            page.IsHomePage = true;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // Helper methods
        private List<BuilderComponent> MapComponentDtos(List<BuilderComponentDto> dtos, Guid pageId, Guid? parentId)
        {
            var components = new List<BuilderComponent>();
            
            for (int i = 0; i < dtos.Count; i++)
            {
                var dto = dtos[i];
                var component = new BuilderComponent
                {
                    Id = Guid.NewGuid(),
                    Type = dto.Type,
                    Name = dto.Name,
                    Order = dto.Order > 0 ? dto.Order : i,
                    // Use PropsJson if provided (from frontend), otherwise serialize Props dictionary
                    PropsJson = !string.IsNullOrEmpty(dto.PropsJson) 
                        ? dto.PropsJson 
                        : (dto.Props != null && dto.Props.Any() 
                            ? JsonSerializer.Serialize(dto.Props) 
                            : "{}"),
                    DesktopStyles = dto.Styles?.Desktop,
                    TabletStyles = dto.Styles?.Tablet,
                    MobileStyles = dto.Styles?.Mobile,
                    CustomCSS = dto.Styles?.CustomCSS,
                    Locked = dto.Locked,
                    Hidden = dto.Hidden,
                    PageId = pageId,
                    ParentId = parentId
                };

                if (dto.Children != null && dto.Children.Any())
                {
                    component.Children = MapComponentDtos(dto.Children, pageId, component.Id);
                }

                components.Add(component);
            }

            return components;
        }

        private void AssignChildren(BuilderComponent parent, List<BuilderComponent> allComponents)
        {
            parent.Children = allComponents.Where(c => c.ParentId == parent.Id).ToList();
            foreach (var child in parent.Children)
            {
                AssignChildren(child, allComponents);
            }
        }

        private List<BuilderComponent> CloneComponents(List<BuilderComponent> originals, Guid pageId, Guid? parentId)
        {
            var cloned = new List<BuilderComponent>();

            foreach (var original in originals)
            {
                var component = new BuilderComponent
                {
                    Id = Guid.NewGuid(),
                    Type = original.Type,
                    Name = original.Name,
                    Order = original.Order,
                    PropsJson = original.PropsJson,
                    DesktopStyles = original.DesktopStyles,
                    TabletStyles = original.TabletStyles,
                    MobileStyles = original.MobileStyles,
                    CustomCSS = original.CustomCSS,
                    Locked = original.Locked,
                    Hidden = original.Hidden,
                    PageId = pageId,
                    ParentId = parentId
                };

                if (original.Children != null && original.Children.Any())
                {
                    component.Children = CloneComponents(original.Children, pageId, component.Id);
                }

                cloned.Add(component);
            }

            return cloned;
        }
    }
}