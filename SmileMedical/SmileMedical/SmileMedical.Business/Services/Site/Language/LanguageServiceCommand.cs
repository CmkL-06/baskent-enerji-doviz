using AutoMapper;
using SmileMedical.Business.Exceptions;
using SmileMedical.Business.Infrastructure.Site.Language;
using SmileMedical.Business.Services.Permission;
using SmileMedical.Data.Contexts;
using SmileMedical.Entity.Modals.RequestModals.Site.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Services.Site.Language
{
    public class LanguageServiceCommand : ILanguageServiceCommand
    {
        private readonly SmileMedicalDbContext _dbContext;
        private readonly ValidationService _validationService;
        private readonly IMapper mapper;

        public LanguageServiceCommand(SmileMedicalDbContext dbContext, ValidationService validationService, IMapper mapper)
        {
            _dbContext = dbContext;
            _validationService = validationService;
            this.mapper = mapper;
        }
        public async Task RemoveLanguage(Guid Id)
        {
            var dbLanguage = await _dbContext.Languages.FindAsync(Id);
            if (dbLanguage == null) throw new ApiException(System.Net.HttpStatusCode.NotFound, "Language couldn't be found.");
            if (dbLanguage.IsDefault) throw new ApiException(HttpStatusCode.NotAcceptable, "You can't remove default language.");

            if (_dbContext.Pages.FirstOrDefault(x => x.LanguageId == dbLanguage.Id) is not null)
            {
               
            

            Guid bkId = Guid.NewGuid();
            Entity.Entities.Site.Language bkLanguage = new Entity.Entities.Site.Language
            {
                Id = bkId,
                IsEnabled = false,
                LanguageName = "backup",
                IsDefault = false,
                LanguageCode = "backup",
                FlagUri = "backup",
                CreatedDate = DateTime.UtcNow,
            };
            _dbContext.Languages.Add(bkLanguage);
            await _dbContext.SaveChangesAsync();
            var dbDefaultLanguage = _dbContext.Languages.FirstOrDefault(x => x.IsDefault);
            // CHECKING CONTENTS

            foreach (var menu in _dbContext.Menus.Where(x => x.LanguageId == Id).ToList())
            {
                menu.LanguageId = bkId;
            }

            foreach (var cat in _dbContext.Blog_Categories.Where(x => x.LanguageId == Id).ToList())
            {
                cat.LanguageId = bkId;
            }
            foreach (var page in _dbContext.Pages.Where(x => x.LanguageId == Id).ToList())
            {
                page.LanguageId = bkId;
            }
            foreach (var user in _dbContext.Users.Where(x => x.LanguageCode == dbLanguage.LanguageCode).ToList())
            {
                user.LanguageCode = dbDefaultLanguage.LanguageCode;
            }
            foreach (var tag in _dbContext.Tags.Where(x => x.LanguageId == Id))
            {
                tag.LanguageId = bkId;
            }
            foreach (var slider in _dbContext.Sliders.Where(x => x.LanguageId == Id).ToList())
            {
                slider.LanguageId = bkId;
            }
            }
            _dbContext.Languages.Remove(dbLanguage);
            await _dbContext.SaveChangesAsync();

        }

        public async Task SaveLanguage(rm_savelanguage data)
        {
            var dbLanguage = _dbContext.Languages.FirstOrDefault(x => x.Id == data.Id);

            if (data.IsDefault)
            {
                var dbDefaultLang = _dbContext.Languages.FirstOrDefault(x=>x.IsDefault);
                if (dbDefaultLang != null) dbDefaultLang.IsDefault = false;
            }
         
            if (dbLanguage == null)
            {
                //adding new language & page
                var lang = mapper.Map<Entity.Entities.Site.Language>(data);
                _dbContext.Languages.Add(lang);

                Entity.Entities.Site.Page.Page nPage = new Entity.Entities.Site.Page.Page
                {
                    Slug = $"{Tools.tools_string.GenerateSlug(lang.LanguageName)}-home",
                    IsHomePage = true,
                    LanguageId = lang.Id,
                    LanguageCode = lang.LanguageCode,
                    Title = $"{lang.LanguageName} Home",
                    Description = "",
                    Status = Entity.Entities.Site.Page.PageStatus.Published
                };
                _dbContext.Pages.Add(nPage);


            }
            else _dbContext.Entry(dbLanguage).CurrentValues.SetValues(data);

            await _dbContext.SaveChangesAsync();
        }
    }
}
