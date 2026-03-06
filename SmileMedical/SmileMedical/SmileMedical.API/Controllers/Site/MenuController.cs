using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmileMedical.Business.Infrastructure.Site.Menu;
using SmileMedical.Entity.Modals.RequestModals.Site.Menu;
using SmileMedical.Entity.Modals.ViewModals.Menu;

namespace SmileMedical.API.Controllers.Site
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class MenuController : ControllerBase
    {
        private readonly IMenuServiceCommand _menuServiceCommand;
        private readonly IMenuServiceQuery _menuServiceQuery;

        public MenuController(IMenuServiceCommand menuServiceCommand, IMenuServiceQuery menuServiceQuery)
        {
            _menuServiceCommand = menuServiceCommand;
            _menuServiceQuery = menuServiceQuery;
        }

        [HttpPost]
        public async Task SaveMenu(rm_menu_save data)
        {
            await _menuServiceCommand.SaveMenu(data);
        }
        [HttpPost("delete")]
        public async Task DeleteMenu([FromBody] Guid menuId)
        {
            await _menuServiceCommand.DeleteMenu(menuId);
        }
        [HttpPost("item")]
        public async Task SaveMenuItem(rm_menuitem_save data)
        {
            await _menuServiceCommand.SaveMenuItem(data);
        }
        [HttpPost("item/delete")]
        public async Task DeleteMenuItem([FromBody] Guid menuItemId)
        {
            await _menuServiceCommand.DeleteMenuItem(menuItemId);
        }

        [HttpGet("admin")]

        public async Task<List<vm_menu>> GetMenus([FromQuery] rm_menu data)
        {
            return await _menuServiceQuery.GetMenus(data);
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<List<vm_menu_guest>> GetMenus_Guest([FromQuery] rm_menu_guest data)
        {
            return await _menuServiceQuery.GetMenus_Guest(data);
        }

        [HttpGet("admin/item")]
        public async Task<List<vm_menu_item>> GetMenuItems([FromQuery]rm_menuitem data)
        {
            return await _menuServiceQuery.GetMenuItems(data);
        }
        [HttpGet("item")]
        [AllowAnonymous]
        public async Task<List<vm_menu_item_guest>> GetMenuItems_Guest([FromQuery] rm_menuitem_guest data)
        {
            return await _menuServiceQuery.GetMenuItems_Guest(data);
        }
    }
}
