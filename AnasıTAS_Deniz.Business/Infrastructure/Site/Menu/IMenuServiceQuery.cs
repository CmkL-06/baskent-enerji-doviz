using AnasıTAS_Deniz.Entity.Modals.RequestModals.Site.Menu;
using AnasıTAS_Deniz.Entity.Modals.ResponseModals.Site.Menu;
using AnasıTAS_Deniz.Entity.Modals.ViewModals.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Infrastructure.Site.Menu
{
    public interface IMenuServiceQuery
    {
       Task <List<vm_menu>> GetMenus(rm_menu data );
       Task <List<vm_menu_guest>> GetMenus_Guest(rm_menu_guest data );
        Task<List<vm_menu_item>> GetMenuItems(rm_menuitem data);
        Task<List<vm_menu_item_guest>> GetMenuItems_Guest(rm_menuitem_guest data);
    }
}
