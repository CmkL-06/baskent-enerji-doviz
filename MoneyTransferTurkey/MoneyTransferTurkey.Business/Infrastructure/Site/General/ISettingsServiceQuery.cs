using MoneyTransferTurkey.Entity.Modals.ResponseModals.Site.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Infrastructure.Site.General
{
    public interface ISettingsServiceQuery
    {
        vm_settings GetSiteSettings();
    }
}
