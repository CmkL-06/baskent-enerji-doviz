using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.Site.Menu;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Business.Tools;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.Blog;
using BaskentEnerji.Entity.Entities.Site.Menu;
using BaskentEnerji.Entity.Modals.RequestModals.General;
using BaskentEnerji.Entity.Modals.RequestModals.Site.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.Site.Menu
{
    public class MenuServiceCommand : IMenuServiceCommand
    {
        private readonly BaskentEnerjiDbContext _dbContext;
        private readonly ValidationService _validationService;
        private readonly IMapper mapper;

        public MenuServiceCommand(
            BaskentEnerjiDbContext dbContext,
            ValidationService validationService,
            IMapper mapper)

        {
            _dbContext = dbContext;
            _validationService = validationService;
            this.mapper = mapper;
        }

        public async Task SaveMenu(rm_menu_save data)
        {
            if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
            if (data.Name == null) throw new ApiException(HttpStatusCode.NotAcceptable, "Please define a menu name.");
            var dbMenu = await _dbContext.Menus.FindAsync(data.Id);
            if (dbMenu == null)
            {
                var newMenu = mapper.Map<Entity.Entities.Site.Menu.Menu>(data);
              
                await _dbContext.Menus.AddAsync(newMenu);
            }
            else
            {
                mapper.Map(data, dbMenu);
            }
            await _dbContext.SaveChangesAsync();
        }



        public async Task DeleteMenu(Guid menuId)
        {
            if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
            if (menuId == Guid.Empty) throw new ApiException(HttpStatusCode.NotAcceptable, "Please provide data correctly.");
            var dbMenu = await _dbContext.Menus.FindAsync(menuId);
            if (dbMenu == null) throw new ApiException(HttpStatusCode.NotFound, "Menu couldn't be found");
            _dbContext.Menus.Remove(dbMenu);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveMenuItem(rm_menuitem_save data)
        {
            if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
            if (data.Name == null) throw new ApiException(HttpStatusCode.NotAcceptable, "Please define a menuitem name.");
            if (data.MenuId == Guid.Empty) throw new ApiException(HttpStatusCode.NotAcceptable, "Please define a menu");
            if (await _dbContext.Menus.FindAsync(data.MenuId) == null) throw new ApiException(HttpStatusCode.NotFound, "Menu couldn't be found");
           
          
            var dbMenuItem = await _dbContext.Menu_Items.FindAsync(data.Id);
            if (dbMenuItem == null)
            {
                var newMenuItem = mapper.Map<Entity.Entities.Site.Menu.Menu_Item>(data);
                newMenuItem.SeoLink = tools_string.GenerateSlug(string.IsNullOrEmpty(newMenuItem.SeoTitle) ? newMenuItem.Name : newMenuItem.SeoTitle);
              //  if (data.IsCustomLink && string.IsNullOrEmpty(data.CustomLink)) newMenuItem.SeoLink = data.CustomLink;
                await _dbContext.Menu_Items.AddAsync(newMenuItem);
            }
            else
            {
                dbMenuItem.SeoLink = tools_string.GenerateSlug(string.IsNullOrEmpty(dbMenuItem.SeoTitle) ? dbMenuItem.Name : dbMenuItem.SeoTitle);
              //  if (data.IsCustomLink && string.IsNullOrEmpty(data.CustomLink)) dbMenuItem.SeoLink = data.CustomLink;
                mapper.Map(data, dbMenuItem);
            }
            await _dbContext.SaveChangesAsync();

           
        }

        public async Task DeleteMenuItem(Guid menuItemId)
        {
            if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
          
            if (menuItemId == Guid.Empty) throw new ApiException(HttpStatusCode.NotAcceptable, "Please provide data correctly.");
            var dbMenuItem = await _dbContext.Menu_Items.FindAsync(menuItemId);
            if (dbMenuItem == null) throw new ApiException(HttpStatusCode.NotFound, "Menu couldn't be found");
            _dbContext.Menu_Items.Remove(dbMenuItem);
            await _dbContext.SaveChangesAsync();
        }

    }
}
