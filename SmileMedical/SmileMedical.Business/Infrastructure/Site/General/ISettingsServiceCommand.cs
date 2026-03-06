using SmileMedical.Entity.Modals.RequestModals.Site.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Infrastructure.Site.General
{
    public interface ISettingsServiceCommand
    {
        Task SaveSettings(rm_settings_save data);
    }
}
