using SmileMedical.Entity.Modals.RequestModals.General;
using SmileMedical.Entity.Modals.RequestModals.Site.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Infrastructure.Site.Menu
{
    public interface IMenuServiceCommand
    {
        Task SaveMenu(rm_menu_save data);
        Task DeleteMenu(Guid menuId);
      
        Task SaveMenuItem(rm_menuitem_save data);
        Task DeleteMenuItem(Guid menuItemId);
    }
}
