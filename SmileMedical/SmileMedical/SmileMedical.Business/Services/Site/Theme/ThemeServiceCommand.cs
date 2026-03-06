using Microsoft.EntityFrameworkCore;
using SmileMedical.Business.Exceptions;
using SmileMedical.Business.Infrastructure.Site.Theme;
using SmileMedical.Business.Services.Permission;
using SmileMedical.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Services.Site.Theme
{
    public class ThemeServiceCommand : IThemeServiceCommand
    {
        private readonly SmileMedicalDbContext _dbContext;
        private readonly ValidationService _validationService;

        public ThemeServiceCommand(SmileMedicalDbContext dbContext, ValidationService validationService)
        {
            _dbContext = dbContext;
            _validationService = validationService;
        }
        public async Task DeleteTheme(Guid themeId)
        {
            if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
            await _dbContext.Themes.Where(x => x.Id == themeId).ExecuteDeleteAsync();
            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveTheme(Entity.Entities.Site.Theme data)
        {
            if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");
            var dbExistingData =  _dbContext.Themes.FirstOrDefault(x => x.Name == data.Name);
            if (dbExistingData != null) _dbContext.Entry(dbExistingData).CurrentValues.SetValues(data);
            else _dbContext.Themes.Add(data);


            await _dbContext.SaveChangesAsync();
        }
    }
}
