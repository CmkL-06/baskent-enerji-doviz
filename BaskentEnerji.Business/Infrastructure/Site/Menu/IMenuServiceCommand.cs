using BaskentEnerji.Entity.Modals.RequestModals.General;
using BaskentEnerji.Entity.Modals.RequestModals.Site.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.Site.Menu
{
    public interface IMenuServiceCommand
    {
        Task SaveMenu(rm_menu_save data);
        Task DeleteMenu(Guid menuId);
      
        Task SaveMenuItem(rm_menuitem_save data);
        Task DeleteMenuItem(Guid menuItemId);
    }
}
