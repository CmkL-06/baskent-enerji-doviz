using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmileMedical.Business.Exceptions;
using SmileMedical.Business.Infrastructure.Blog.Article;
using SmileMedical.Business.Services.Permission;
using SmileMedical.Business.Tools;
using SmileMedical.Data.Contexts;
using SmileMedical.Entity.Entities.Blog;
using SmileMedical.Entity.Entities.Site;
using SmileMedical.Entity.Modals.RequestModals.Blog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SmileMedical.Business.Services.Blog.Article
{
    public class Blog_ArticleServiceCommand : IBlog_ArticleServiceCommand
    {
        private readonly SmileMedicalDbContext _dbContext;
        private readonly IMapper mapper;
        private readonly ValidationService _validationService;

        public Blog_ArticleServiceCommand(SmileMedicalDbContext dbContext, IMapper mapper, ValidationService validationService)
        {
            _dbContext = dbContext;
            this.mapper = mapper;
            _validationService = validationService;
        }

        public async Task SaveArticle(rm_savearticle data)
        {
            if (!await _validationService.IsStaff())
                throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            var dbArticle = await _dbContext.Blog_Articles.FindAsync(data.Id);
            
            Blog_Article_Category dbAnyCat = null;
            if (dbArticle != null) dbAnyCat = _dbContext.Blog_Article_Categories.Where(x => x.ArticleId == dbArticle.Id).Include(x => x.Category).FirstOrDefault();

            Guid langId = _dbContext.Languages.FirstOrDefault(x => x.IsEnabled).Id;
            if (dbAnyCat != null) langId = dbAnyCat.Category.LanguageId;
            if (dbArticle == null)
            {
                var newArticle = mapper.Map<Blog_Article>(data);
                newArticle.SeoLink = tools_string.GenerateSlug(string.IsNullOrEmpty(newArticle.SeoTitle) ? newArticle.Title : newArticle.SeoTitle);
                newArticle.AuthorId = Guid.Parse(_validationService.GetUserID());
                await _dbContext.Blog_Articles.AddAsync(newArticle);

                await AssignCategoriesToArticle(newArticle.Id, data.Categories);
                await AssignTagsToArticle(newArticle.Id, data.Tags, langId);
            }
            else
            {
                mapper.Map(data, dbArticle);
                dbArticle.SeoLink = tools_string.GenerateSlug(string.IsNullOrEmpty(dbArticle.SeoTitle) ? dbArticle.Title : dbArticle.SeoTitle);

                await UpdateArticleCategories(dbArticle.Id, data.Categories);
                await UpdateArticleTags(dbArticle.Id, data.Tags, langId);
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task AssignCategoriesToArticle(Guid articleId, List<Guid> categoryIds)
        {
            var articleCategories = categoryIds.Select(categoryId => new Blog_Article_Category
            {
                ArticleId = articleId,
                CategoryId = categoryId
            });

            await _dbContext.Blog_Article_Categories.AddRangeAsync(articleCategories);
        }

        private async Task AssignTagsToArticle(Guid articleId, List<string> tagNames, Guid LangId)
        {
            var existingTags = await _dbContext.Tags.Where(t => tagNames.Contains(t.Name)).ToListAsync();
            var newTagNames = tagNames.Except(existingTags.Select(t => t.Name)).ToList();

            var newTags = newTagNames.Select(name => new Tag
            {
                Name = name,
                SeoTitle = name,
                SeoLink = tools_string.GenerateSlug(name),
                Content = string.Empty,
                LanguageId = LangId
            }).ToList();

            _dbContext.Tags.AddRange(newTags);
            await _dbContext.SaveChangesAsync(); // Save to generate IDs for new tags

            var allTags = existingTags.Concat(newTags).ToList();
            var articleTags = allTags.Select(tag => new Blog_Article_Tag
            {
                ArticleId = articleId,
                TagId = tag.Id
            }).ToList();

            await _dbContext.Blog_Article_Tags.AddRangeAsync(articleTags);
        }
        private async Task UpdateArticleCategories(Guid articleId, List<Guid> categoryIds)
        {
            var dbCurrentArticleCats = await _dbContext.Blog_Article_Categories
                .Where(x => x.ArticleId == articleId).ToListAsync();

            var categoriesToRemove = dbCurrentArticleCats.Where(x => !categoryIds.Contains(x.CategoryId)).ToList();
            var categoriesToAdd = categoryIds.Where(cat => dbCurrentArticleCats.All(c => c.CategoryId != cat))
                .Select(cat => new Blog_Article_Category
                {
                    ArticleId = articleId,
                    CategoryId = cat
                }).ToList();

            _dbContext.Blog_Article_Categories.RemoveRange(categoriesToRemove);
            await _dbContext.Blog_Article_Categories.AddRangeAsync(categoriesToAdd);
        }

        private async Task UpdateArticleTags(Guid articleId, List<string> tagNames, Guid LangId)
        {
            var dbCurrentArticleTags = await _dbContext.Blog_Article_Tags
                .Include(x => x.Tag)
                .Where(x => x.ArticleId == articleId)
                .ToListAsync();

            // Remove tags that are no longer associated with the article
            var tagsToRemove = dbCurrentArticleTags.Where(x => !tagNames.Contains(x.Tag.Name)).ToList();
            _dbContext.Blog_Article_Tags.RemoveRange(tagsToRemove);

            // Get existing tags
            var existingTags = await _dbContext.Tags.Where(t => tagNames.Contains(t.Name)).ToListAsync();
            var existingTagNames = existingTags.Select(t => t.Name).ToList();

            // Create new tags
            var newTagNames = tagNames.Except(existingTagNames).ToList();
            var newTags = newTagNames.Select(name => new Tag
            {
                Name = name,
                SeoTitle = name,
                SeoLink = tools_string.GenerateSlug(name),
                Content = string.Empty,
                LanguageId = LangId,
            }).ToList();

            // Add new tags to the context and save changes
            _dbContext.Tags.AddRange(newTags);
            await _dbContext.SaveChangesAsync();

            // Combine existing and new tags
            var allTags = existingTags.Concat(newTags).ToList();

            // Create new article-tag associations
            var existingArticleTagIds = dbCurrentArticleTags.Select(t => t.TagId).ToList();
            var tagsToAdd = allTags.Where(tag => !existingArticleTagIds.Contains(tag.Id))
                .Select(tag => new Blog_Article_Tag
                {
                    ArticleId = articleId,
                    TagId = tag.Id
                }).ToList();

            // Add new associations
            await _dbContext.Blog_Article_Tags.AddRangeAsync(tagsToAdd);
        }

        public async Task DeleteArticle(Guid articleId)
        {
            if (!await _validationService.IsStaff())
                throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            if (articleId == Guid.Empty)
                throw new ApiException(HttpStatusCode.NotAcceptable, "Article id shouldn't be empty.");

            var dbArticle = await _dbContext.Blog_Articles.FindAsync(articleId);
            if (dbArticle == null)
                throw new ApiException(HttpStatusCode.NotFound, "Article couldn't be found.");

            var dbArticleCats = await _dbContext.Blog_Article_Categories.Where(x => x.ArticleId == articleId).ToListAsync();
            if (dbArticleCats.Count > 0) _dbContext.Blog_Article_Categories.RemoveRange(dbArticleCats);

            _dbContext.Blog_Articles.Remove(dbArticle);
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddCategory(List<rm_article_addcategory> data)
        {
            if (!await _validationService.IsStaff())
                throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            foreach (var category in data)
            {
                await _dbContext.Blog_Article_Categories.AddAsync(new Blog_Article_Category
                {
                    ArticleId = category.ArticleId,
                    CategoryId = category.CategoryId,
                });
            }
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddComment(rm_article_comment data, HttpContext httpContext)
        {

            string visitorIp = Tools.tools_string.GetIpAddress(httpContext);

            var dbCheckBlackList = _dbContext.BlackList.FirstOrDefault(x => x.IpAdress == visitorIp);
            if (dbCheckBlackList != null) throw new ApiException(HttpStatusCode.Forbidden, "You're blacklisted.");

            var dbCheckSpam = _dbContext.Blog_Article_Comments.Where(x => x.IpAdress == visitorIp && x.ArticleId == data.ArticleId).ToList();
            if (dbCheckSpam.Count >= 2) throw new ApiException(HttpStatusCode.Forbidden, "Stop spamming.");

            if (data.UserId.HasValue && !await _validationService.HasPermissionAsync(data.UserId.Value)) throw new ApiException(HttpStatusCode.Unauthorized, ("You have no permission to do this."));

            var dbArticle = await _dbContext.Blog_Articles.FindAsync(data.ArticleId);
            if (dbArticle == null) throw new ApiException(HttpStatusCode.NotFound, "Article couldn't be found.");

            _dbContext.Blog_Article_Comments.Add(mapper.Map<Blog_Article_Comment>(data));
            dbArticle.Views++;

            await _dbContext.SaveChangesAsync();

        }

        public async Task RemoveComment(Guid ArticleId)
        {
            if (!await _validationService.IsAdminAsync()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            var dbComment = _dbContext.Blog_Article_Comments.FirstOrDefault(x => x.ArticleId == ArticleId);
            _dbContext.Blog_Article_Comments.Remove(dbComment);
            await _dbContext.SaveChangesAsync();

        }
    }
}
