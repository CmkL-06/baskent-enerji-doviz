using MoneyTransferTurkey.Entity.Modals.RequestModals.Site.Tag;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Infrastructure.Site.Tag
{
    public interface ITagServiceCommand
    {
        Task SaveTag(rm_savetag data);
        Task AddTag_Article(rm_addtag_article data);
        Task AddTag_Category(rm_addtag_category data);
        Task RemoveTag(Guid tagId);
        Task RemoveTagFromArticle(Guid id);
        Task RemoveTagFromCategory(Guid id);
    }
}
