using MoneyTransferTurkey.Business.Infrastructure.Site.Language;
using MoneyTransferTurkey.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Services.Site.Language
{
    public class LanguageServiceQuery : ILanguageServiceQuery
    {
        private readonly MoneyTransferTurkeyDbContext _dbContext;

        public LanguageServiceQuery(MoneyTransferTurkeyDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public Entity.Entities.Site.Language GetDefaultLanguage()
        {
            return _dbContext.Languages.FirstOrDefault(x => x.IsDefault);
        }

        public Entity.Entities.Site.Language GetLanguageById(Guid Id)
        {
           return _dbContext.Languages.FirstOrDefault(x => x.Id == Id);
        }

        public List<Entity.Entities.Site.Language> GetLanguages()
        {

            return _dbContext.Languages.ToList();
        }
    }
}
