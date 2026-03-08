using AutoMapper;
using Microsoft.EntityFrameworkCore;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.Site.Tag;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Business.Tools;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.Blog;
using BaskentEnerji.Entity.Entities.Site;
using BaskentEnerji.Entity.Modals.RequestModals.Site.Tag;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.Site.Tag
{
    public class TagServiceCommand : ITagServiceCommand
    {
        private readonly BaskentEnerjiDbContext _dbContext;
        private readonly IMapper mapper;
        private readonly ValidationService _validationService;

        public TagServiceCommand(BaskentEnerjiDbContext dbContext, IMapper mapper, ValidationService validationService)
        {
            _dbContext = dbContext;
            this.mapper = mapper;
            _validationService = validationService;
        }

        public async Task SaveTag(rm_savetag data)
        {
            if (!await _validationService.IsStaff())
                throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            var dbTag = await _dbContext.Tags.FindAsync(data.Id);
            if (dbTag == null)
            {
                var newTag = mapper.Map<Entity.Entities.Site.Tag>(data);
                newTag.SeoLink = tools_string.GenerateSlug(string.IsNullOrEmpty(newTag.SeoTitle) ? newTag.Name : newTag.SeoTitle);
                await _dbContext.Tags.AddAsync(newTag);
            }
            else mapper.Map(data, dbTag);

            await _dbContext.SaveChangesAsync();
        }
        public async Task RemoveTag(Guid tagId)
        {
            if (!await _validationService.IsStaff())
                throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            if (tagId == Guid.Empty)
                throw new ApiException(HttpStatusCode.NotAcceptable, "Article id shouldn't be empty.");

            var dbTag = await _dbContext.Tags.FindAsync(tagId);
            if (dbTag == null)
                throw new ApiException(HttpStatusCode.NotFound, "Tag couldn't be found.");

            var TagCats = await _dbContext.Blog_Article_Tags
                .Where(x => x.TagId == tagId).ToListAsync();
            //    if (dbTag != null) await _dbContext.Tags.Where(x => x.Id == tagId).ExecuteDeleteAsync();
            if (dbTag != null)
            {
                _dbContext.Tags.Remove(dbTag);
                await _dbContext.SaveChangesAsync();
            }

        }

        public async Task AddTag_Category(rm_addtag_category data)
        {
            if (!await _validationService.IsStaff())
                throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            if (data.TagId == Guid.Empty || data.CategoryId == Guid.Empty) throw new ApiException(HttpStatusCode.NotAcceptable, "Please post data correctly.");
            var dbTag = await _dbContext.Tags.FindAsync(data.TagId);
            if (dbTag == null) throw new ApiException(HttpStatusCode.NotFound, "Tag couldn't be found");
            var dbCategory = await _dbContext.Blog_Categories.FindAsync(data.CategoryId);
            if (dbCategory == null) throw new ApiException(HttpStatusCode.NotFound, "Category couldn't be found");
            var dbCatTag = _dbContext.Blog_Category_Tags.Where(x => x.TagId == data.TagId && x.CategoryId == data.CategoryId);
            if (dbCatTag.Any()) throw new ApiException(HttpStatusCode.AlreadyReported, "This tag already related with the category.");

            await _dbContext.Blog_Category_Tags.AddAsync(new Blog_Category_Tag { CategoryId = data.CategoryId, TagId = data.TagId });
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddTag_Article(rm_addtag_article data)
        {
            if (!await _validationService.IsStaff())
                throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            if (data.TagId == Guid.Empty || data.ArticleId == Guid.Empty) throw new ApiException(HttpStatusCode.NotAcceptable, "Please post data correctly.");
            var dbTag = await _dbContext.Tags.FindAsync(data.TagId);
            if (dbTag == null) throw new ApiException(HttpStatusCode.NotFound, "Tag couldn't be found");
            var dbArticle = await _dbContext.Blog_Articles.FindAsync(data.ArticleId);
            if (dbArticle == null) throw new ApiException(HttpStatusCode.NotFound, "Article couldn't be found");
            var dbCatTag = _dbContext.Blog_Article_Tags.Where(x => x.TagId == data.TagId && x.ArticleId == data.ArticleId);
            if (dbCatTag.Any()) throw new ApiException(HttpStatusCode.AlreadyReported, "This tag already related with the article.");

            await _dbContext.Blog_Article_Tags.AddAsync(new Blog_Article_Tag { ArticleId = data.ArticleId, TagId = data.TagId });
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveTagFromArticle(Guid id)
        {
            await _dbContext.Blog_Article_Tags.Where(x => x.Id == id).ExecuteDeleteAsync();
        }

        public async Task RemoveTagFromCategory(Guid id)
        {
            await _dbContext.Blog_Category_Tags.Where(x => x.Id == id).ExecuteDeleteAsync();
        }
    }
}
