using Microsoft.EntityFrameworkCore;
using SmileMedical.Business.Infrastructure.Site.Theme;
using SmileMedical.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Services.Site.Theme
{
    public class ThemeServiceQuery : IThemeServiceQuery
    {
        private readonly SmileMedicalDbContext _dbContext;

        public ThemeServiceQuery(SmileMedicalDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<Entity.Entities.Site.Theme> GetThemeById(Guid Id)
        {
            return await _dbContext.Themes.FindAsync(Id);
        }

        public async Task<IEnumerable<Entity.Entities.Site.Theme>> GetThemes()
        {
            return await _dbContext.Themes.ToListAsync();
        }
    }
}
