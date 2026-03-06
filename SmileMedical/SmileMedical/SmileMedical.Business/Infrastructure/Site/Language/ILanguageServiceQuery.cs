using SmileMedical.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Infrastructure.Site.Language
{
    public interface ILanguageServiceQuery
    {
        List<Entity.Entities.Site.Language> GetLanguages();
        Entity.Entities.Site.Language GetLanguageById(Guid Id);
        Entity.Entities.Site.Language GetDefaultLanguage();
    }
}
