using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmileMedical.Business.Infrastructure.Site.General;
using SmileMedical.Business.Infrastructure.Site.Language;
using SmileMedical.Entity.Modals.RequestModals.Site.General;
using SmileMedical.Entity.Modals.RequestModals.Site.Language;
using SmileMedical.Entity.Modals.ResponseModals.Site.General;

namespace SmileMedical.API.Controllers.Site
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SiteController : ControllerBase
    {
        private readonly ILanguageServiceQuery _languageServiceQuery;
        private readonly ILanguageServiceCommand _languageServiceCommand;
        private readonly ISettingsServiceCommand _settingsServiceCommand;
        private readonly ISettingsServiceQuery _settingsServiceQuery;

        public SiteController(ILanguageServiceQuery languageServiceQuery, ILanguageServiceCommand languageServiceCommand, ISettingsServiceCommand settingsServiceCommand, ISettingsServiceQuery settingsServiceQuery)
        {
            _languageServiceQuery = languageServiceQuery;
            _languageServiceCommand = languageServiceCommand;
            _settingsServiceCommand = settingsServiceCommand;
            _settingsServiceQuery = settingsServiceQuery;
        }

        [HttpGet("Languages")]
        public List<Entity.Entities.Site.Language> getLanguages()
        {
            return _languageServiceQuery.GetLanguages();
        }

        [HttpGet("Language")]
        public Entity.Entities.Site.Language getLanguageById([FromQuery] Guid Id)
        {
            return _languageServiceQuery.GetLanguageById(Id);
        }

        [HttpGet("Language/Default")]
        public Entity.Entities.Site.Language getDefaultLanguage()
        {
            return _languageServiceQuery.GetDefaultLanguage();
        }

        [HttpPost("Language")]
        public async Task saveLanguage(rm_savelanguage data)
        {
            await _languageServiceCommand.SaveLanguage(data);
        }

        [HttpPost("Language/remove")]
        public async Task removeLanguage([FromBody] Guid Id)
        {
            await _languageServiceCommand.RemoveLanguage(Id);
        }

        [HttpGet ("Settings")]
        public vm_settings getSiteSettings()
        {
            return _settingsServiceQuery.GetSiteSettings();
        }

        [HttpPost ("Settings")]
        public async Task SaveSiteSettings(rm_settings_save data)
        {
            await _settingsServiceCommand.SaveSettings(data);
        }

    }
}