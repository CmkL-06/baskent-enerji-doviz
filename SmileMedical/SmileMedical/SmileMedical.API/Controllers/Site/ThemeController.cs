using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmileMedical.Business.Infrastructure.Site.Theme;

namespace SmileMedical.API.Controllers.Site
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class ThemeController : ControllerBase
    {
        private readonly IThemeServiceCommand _themeServiceCommand;
        private readonly IThemeServiceQuery _themeServiceQuery;

        public ThemeController(IThemeServiceCommand themeServiceCommand, IThemeServiceQuery themeServiceQuery)
        {
            _themeServiceCommand = themeServiceCommand;
            _themeServiceQuery = themeServiceQuery;
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<IEnumerable<Entity.Entities.Site.Theme>> GetThemes()
        {
            return await _themeServiceQuery.GetThemes();
        }


        [HttpGet("id")]
        [AllowAnonymous]
        public async Task<Entity.Entities.Site.Theme> GetThemeById([FromQuery]Guid themeId)
        {
            return await _themeServiceQuery.GetThemeById(themeId);
        }

        [HttpPost]
      
        public async Task SaveTheme(Entity.Entities.Site.Theme data)
        {
            await _themeServiceCommand.SaveTheme(data);
        }

        [HttpPost("delete")]
       
        public async Task DeleteTheme([FromBody] Guid themeId)
        {
            await _themeServiceCommand.DeleteTheme(themeId);
        }
    }
}
