using Microsoft.EntityFrameworkCore;
using AnasıTAS_Deniz.Business.Infrastructure.Site.Theme;
using AnasıTAS_Deniz.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Services.Site.Theme
{
    public class ThemeServiceQuery : IThemeServiceQuery
    {
        private readonly AnasıTAS_DenizDbContext _dbContext;

        public ThemeServiceQuery(AnasıTAS_DenizDbContext dbContext)
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
