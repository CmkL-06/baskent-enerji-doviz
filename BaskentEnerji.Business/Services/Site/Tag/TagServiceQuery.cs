using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.Site.Tag;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.Site.Tag
{
    public class TagServiceQuery : ITagServiceQuery
    {
        private readonly BaskentEnerjiDbContext _dbContext;

        public TagServiceQuery(BaskentEnerjiDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public List<Entity.Entities.Site.Tag> GetTagsByArticleIdWithCategory_old(Guid ArticleId)
        {
            if (ArticleId == null || ArticleId == Guid.Empty) throw new ApiException(HttpStatusCode.NotAcceptable, "Please post data correctly.");

            List<Entity.Entities.Site.Tag> dataTags = new List<Entity.Entities.Site.Tag>();
            var dbTagsByArticle = _dbContext.Blog_Article_Tags.Where(x => x.Article.Id == ArticleId).ToList();
            if (dbTagsByArticle.Count == 0) return null;
            foreach (var articletag in dbTagsByArticle)
            {
                var dbTag = _dbContext.Tags.Where(x => x.Id == articletag.Id).FirstOrDefault();
                dataTags.Add(dbTag);
            }
            var dbCategories = _dbContext.Blog_Article_Categories.Where(x => x.ArticleId == ArticleId).ToList();
            if (dbCategories.Count > 0)
            {
                foreach (var category in dbCategories)
                {
                    var dbCatTags = _dbContext.Blog_Category_Tags.Where(x => x.CategoryId == category.Id).ToList();
                    foreach (var dbtag in dbCatTags)
                    {
                        var dbTag = _dbContext.Tags.Where(x => x.Id == dbtag.TagId).FirstOrDefault();
                        dataTags.Add(dbTag);
                    }
                }
            }
            return dataTags.Distinct().ToList();

        }
        public List<Entity.Entities.Site.Tag> GetTagsByArticleIdWithCategory(Guid ArticleId)
        {
            if (ArticleId == Guid.Empty)
                throw new ApiException(HttpStatusCode.NotAcceptable, "Invalid article ID.");

            var tags = _dbContext.Blog_Article_Tags
                .Where(at => at.ArticleId == ArticleId)
                .Select(at => at.Tag)
                .Union(
                    _dbContext.Blog_Article_Categories
                        .Where(ac => ac.ArticleId == ArticleId)
                        .SelectMany(ac => ac.Category.CategoryTags)
                        .Select(ct => ct.Tag)
                )
                .Distinct()
                .ToList();

            return tags.Any() ? tags : null;

        }
        public List<Entity.Entities.Site.Tag> GetTagsByArticleIdWithoutCategory(Guid ArticleId)
        {
            if (ArticleId == Guid.Empty)
                throw new ApiException(HttpStatusCode.NotAcceptable, "Invalid article ID.");
            return _dbContext.Blog_Article_Tags
                .Where(x => x.ArticleId == ArticleId).Select(x => x.Tag).ToList();
        }

        public List<Entity.Entities.Site.Tag> GetTagsByCategoryId(Guid CategoryId)
        {
            if (CategoryId == Guid.Empty)
                throw new ApiException(HttpStatusCode.NotAcceptable, "Invalid article ID.");
            return _dbContext.Blog_Category_Tags
                .Where(x => x.CategoryId == CategoryId).Select(x => x.Tag).ToList();
        }

        public List<Entity.Entities.Site.Tag> GetTagsByLanguageId(Guid LanguageId)
        {
            if (LanguageId == Guid.Empty)
                throw new ApiException(HttpStatusCode.NotAcceptable, "Invalid article ID.");
            return _dbContext.Tags
                .Where(x => x.LanguageId == LanguageId).ToList();
        }

        public List<Entity.Entities.Site.Tag> GetTags()
        {
            return _dbContext.Tags.ToList();
        }
    }
}
