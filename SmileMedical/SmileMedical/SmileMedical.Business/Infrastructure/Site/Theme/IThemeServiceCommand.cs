using SmileMedical.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Infrastructure.Site.Theme
{
    public interface IThemeServiceCommand
    {
       Task SaveTheme(Entity.Entities.Site.Theme data);
        Task DeleteTheme(Guid themeId);
    }
}
