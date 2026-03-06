using SmileMedical.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Infrastructure.Site.Theme
{
    public interface IThemeServiceQuery
    {
        Task<IEnumerable<Entity.Entities.Site.Theme>> GetThemes();
       Task<Entity.Entities.Site.Theme> GetThemeById(Guid Id);
    }
}
