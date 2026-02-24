using AnasıTAS_Deniz.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Infrastructure.Site.Tag
{
    public interface ITagServiceQuery
    {
        List<Entity.Entities.Site.Tag> GetTagsByArticleIdWithCategory(Guid ArticleId);
        List<Entity.Entities.Site.Tag> GetTagsByArticleIdWithoutCategory(Guid ArticleId);
        List<Entity.Entities.Site.Tag> GetTagsByCategoryId(Guid CategoryId);
        List<Entity.Entities.Site.Tag> GetTagsByLanguageId(Guid LanguageId);
        List<Entity.Entities.Site.Tag> GetTags(); 
    }
}
