using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using SmileMedical.Business.Exceptions;
using SmileMedical.Business.Infrastructure.Site.Menu;
using SmileMedical.Business.Services.Permission;
using SmileMedical.Data.Contexts;
using SmileMedical.Entity.Entities.Site;
using SmileMedical.Entity.Entities.Site.Menu;
using SmileMedical.Entity.Modals.RequestModals.Site.Menu;
using SmileMedical.Entity.Modals.ResponseModals.Site.Menu;
using SmileMedical.Entity.Modals.ViewModals.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Services.Site.Menu
{
    public class MenuServiceQuery : IMenuServiceQuery
    {
        private readonly SmileMedicalDbContext _dbContext;
        private readonly ValidationService _validationService;
        private readonly IMapper mapper;

        public MenuServiceQuery(SmileMedicalDbContext dbContext, ValidationService validationService, IMapper mapper)
        {
            _dbContext = dbContext;
            _validationService = validationService;
            this.mapper = mapper;
        }

        public async Task<List<vm_menu_item>> GetMenuItems(rm_menuitem data)
        {
            if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
            var query = _dbContext.Menu_Items.AsQueryable();

            if (data.MenuId.HasValue) query = query.Where(x => x.MenuId == data.MenuId);
            if (data.IsEnabled.HasValue) query = query.Where(x => x.IsEnabled == data.IsEnabled.Value);

            return await query.ProjectTo<vm_menu_item>(mapper.ConfigurationProvider).OrderByDescending(x => x.Order).ToListAsync();
        }

        public async Task<List<vm_menu_item_guest>> GetMenuItems_Guest(rm_menuitem_guest data)
        {
            var query = _dbContext.Menu_Items.AsQueryable();

            if (data.MenuId.HasValue) query = query.Where(x => x.MenuId == data.MenuId && x.IsEnabled);
            //if (data.IsEnabled.HasValue) query = query.Where(x => x.IsEnabled == data.IsEnabled.Value);

            return await query.ProjectTo<vm_menu_item_guest>(mapper.ConfigurationProvider).OrderByDescending(x => x.Order).ToListAsync();
        }

        public async Task<List<vm_menu>> GetMenus(rm_menu data)
        {
             if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            var query = _dbContext.Menus.AsQueryable();
            query = query.Include(x => x.SubMenus)
              .ThenInclude(subMenu => subMenu.Language);

            query = query.Include(x => x.Language);

            if (!string.IsNullOrEmpty(data.Lang))
                query = query.Where(m => m.Language.LanguageCode == data.Lang);

            if (data.LanguageId.HasValue)
                query = query.Where(m => m.LanguageId == data.LanguageId.Value);

            if (data.IsMain.HasValue && data.IsMain.Value)
                query = query.Where(m => m.ParentMenuId == null || m.ParentMenuId == Guid.Empty);

            if (data.IsEnabled.HasValue)
                query = query.Where(m => m.IsEnabled == data.IsEnabled.Value);

            if (data.IsDisabled.HasValue)
                query = query.Where(m => m.IsEnabled == data.IsDisabled.Value);

            if (data.IsNavbar.HasValue)
                query = query.Where(m => m.IsOnNavbar == data.IsNavbar.Value);

            if (data.IsMobile.HasValue)
                query = query.Where(m => m.IsForMobile == data.IsMobile.Value);

            if (data.IsCategoryMenu.HasValue)
                query = query.Where(m => m.IsCategoryMenu == data.IsCategoryMenu.Value);

            if (data.ParentId.HasValue && data.ParentId != Guid.Empty)
                query = query.Where(m => m.ParentMenuId == data.ParentId.Value);

            var result = await query.ProjectTo<vm_menu>(mapper.ConfigurationProvider)
                                    .OrderByDescending(m => m.CreatedDate)
                                    .ToListAsync();
            return result;
        }

        public async Task<List<vm_menu_guest>> GetMenus_Guest(rm_menu_guest data)
        {
            // Start building the query
            var query = _dbContext.Menus.AsQueryable();

            // Filtering enabled menus
            query = query.Where(x => x.IsEnabled);

            // Include related data without filtering in the Include statements
            query = query.Include(x => x.Language)
                         .Include(x => x.MenuItems)
                         .Include(x => x.SubMenus) // Load SubMenus without filtering
                         .ThenInclude(subMenu => subMenu.Language); // Include any other necessary relations

            // Additional filtering based on input data
            if (!string.IsNullOrEmpty(data.Lang))
                query = query.Where(m => m.Language.LanguageCode == data.Lang);

            if (data.LanguageId != null)
                query = query.Where(m => m.LanguageId == data.LanguageId);

            if (data.IsMain.HasValue)
                query = query.Where(m => m.ParentMenuId == null || m.ParentMenuId == Guid.Empty);

            if (data.IsNavbar.HasValue)
                query = query.Where(m => m.IsOnNavbar == data.IsNavbar.Value);

            if (data.IsFooter.HasValue)
                query = query.Where(m => m.IsOnFooter == data.IsFooter.Value);

            if (data.IsMobile.HasValue)
                query = query.Where(m => m.IsForMobile == data.IsMobile.Value);

            if (data.ParentId.HasValue && data.ParentId != Guid.Empty)
                query = query.Where(m => m.ParentMenuId == data.ParentId.Value);

            if (data.IsCategoryMenu.HasValue)
                query = query.Where(m => m.IsCategoryMenu == data.IsCategoryMenu.Value);

            // Execute the query and get the result
            var result = await query.OrderBy(m => m.Order).ToListAsync();

            // Process each menu item
            foreach (var item in result)
            {
                // If it's a category menu, load associated categories
                if (item.IsCategoryMenu)
                {
                    var catId = item.Blog_CategoryId;
                    item.CustomUrl ="/category/"+  _dbContext.Blog_Categories.FirstOrDefault(x => x.Id == catId)?.SeoLink ?? "";

                    // Fetch parent categories related to the menu's Blog_CategoryId
                    var dbParentCats = await _dbContext.Blog_Categories
                        .Where(x => x.ParentCategoryId == item.Blog_CategoryId).OrderBy(x=> x.Order).ToListAsync();

                    // Add each category as a menu item
                    foreach (var cat in dbParentCats)
                    {
                        Menu_Item mItem = new Menu_Item
                        {
                            Name = cat.Name,
                            SeoLink = $"/category/{cat.SeoLink}",
                            SeoTitle = cat.SeoTitle,
                            Order = cat.Order
                        };
                        item.MenuItems.Add(mItem);
                    }
                }

            }

            // Map the result to vm_menu_guest using AutoMapper
            var mappedResult = mapper.Map<List<vm_menu_guest>>(result);

            // Return the final result
            return mappedResult;
        }

    }
}
