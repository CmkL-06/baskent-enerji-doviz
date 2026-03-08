using MoneyTransferTurkey.Entity.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.RequestModals.Site.Menu
{
    public class rm_menuitem : BaseEntity
    {
        public Guid? MenuId { get; set; }
        public bool? IsEnabled { get; set; }

    }
}
