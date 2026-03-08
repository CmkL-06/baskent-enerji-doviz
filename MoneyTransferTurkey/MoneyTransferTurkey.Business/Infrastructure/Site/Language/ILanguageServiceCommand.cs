using MoneyTransferTurkey.Entity.Entities.Site;
using MoneyTransferTurkey.Entity.Modals.RequestModals.Site.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Infrastructure.Site.Language
{
    public interface ILanguageServiceCommand
    {
        Task SaveLanguage(rm_savelanguage data);
        Task RemoveLanguage (Guid Id);
    }
}
