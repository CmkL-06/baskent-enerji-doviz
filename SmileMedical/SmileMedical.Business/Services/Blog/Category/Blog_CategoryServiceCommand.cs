using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmileMedical.Business.Exceptions;
using SmileMedical.Business.Infrastructure.Blog.Category;
using SmileMedical.Business.Services.Permission;
using SmileMedical.Business.Tools;
using SmileMedical.Data.Contexts;
using SmileMedical.Entity.Entities.Blog;
using SmileMedical.Entity.Entities.Site;
using SmileMedical.Entity.Modals.RequestModals.Blog;
using SmileMedical.Entity.Modals.RequestModals.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Services.Blog
{
    public class Blog_CategoryServiceCommand : IBlog_CategoryServiceCommand
    {
        private readonly SmileMedicalDbContext _dbContext;
        private readonly IMapper mapper;
        private readonly ValidationService _validationService;

        public Blog_CategoryServiceCommand(SmileMedicalDbContext dbContext, IMapper mapper, ValidationService validationService)
        {
            _dbContext = dbContext;
            this.mapper = mapper;
            _validationService = validationService;
        }

    

        public async Task SaveCategory(rm_savecategory data)
        {
            if (!await _validationService.IsStaff())
                throw new ApiException(System.Net.HttpStatusCode.Unauthorized, "You have no permission to do this.");

            var dbCategory = await _dbContext.Blog_Categories.FindAsync(data.Id);
            if (dbCategory == null)
            {
                // means new category
                var newCategory = mapper.Map<Blog_Category>(data);
                newCategory.SeoLink = tools_string.GenerateSlug(string.IsNullOrEmpty(newCategory.SeoTitle) ? newCategory.Name : newCategory.SeoTitle);

                await _dbContext.Blog_Categories.AddAsync(newCategory);
                await AssignTagsToCategory(newCategory.Id, data.Tags, newCategory.LanguageId);
            }
            else
            {
               var mappedData =  mapper.Map(data, dbCategory);
                if (data.ParentCategoryId.HasValue)
                {
                    var dbParent = await _dbContext.Blog_Categories.FindAsync(dbCategory.ParentCategoryId);
                    if (dbParent.ParentCategoryId == data.ParentCategoryId) mappedData.ParentCategoryId = null;
                }
                dbCategory = mappedData;
                dbCategory.SeoLink = Tools.tools_string.GenerateSlug(string.IsNullOrEmpty(dbCategory.SeoTitle) ? dbCategory.Name : dbCategory.SeoTitle);
                await UpdateCategoryTags(mappedData.Id, data.Tags, mappedData.LanguageId);
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task AssignTagsToCategory(Guid categoryId, List<string> tagNames, Guid LangId)
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
            var categoryTags = allTags.Select(tag => new Blog_Category_Tag
            {
                CategoryId = categoryId,
                TagId = tag.Id
            }).ToList();

            await _dbContext.Blog_Category_Tags.AddRangeAsync(categoryTags);
        }
        private async Task UpdateCategoryTags(Guid categoryId, List<string> tagNames, Guid LangId)
        {
            var dbCurrentCategoryTags = await _dbContext.Blog_Category_Tags
                .Include(x => x.Tag)
                .Where(x => x.CategoryId == categoryId)
                .ToListAsync();

            // Remove tags that are no longer associated with the article
            var tagsToRemove = dbCurrentCategoryTags.Where(x => !tagNames.Contains(x.Tag.Name)).ToList();
            _dbContext.Blog_Category_Tags.RemoveRange(tagsToRemove);

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
            var existingCategoryTagIds = dbCurrentCategoryTags.Select(t => t.TagId).ToList();
            var tagsToAdd = allTags.Where(tag => !existingCategoryTagIds.Contains(tag.Id))
                .Select(tag => new Blog_Category_Tag
                {
                    CategoryId = categoryId,
                    TagId = tag.Id
                }).ToList();

            // Add new associations
            await _dbContext.Blog_Category_Tags.AddRangeAsync(tagsToAdd);
        }

        public async Task DeleteCategory(Guid id)
        {
            if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            if (id == Guid.Empty) throw new ApiException(System.Net.HttpStatusCode.NotAcceptable, "Category id shouldn't be empty.");

            var dbCategory = await _dbContext.Blog_Categories.FindAsync(id);
            if (dbCategory == null) throw new ApiException(System.Net.HttpStatusCode.NotFound, "Category couldn't be found.");

            // Checking articles and tags before deleting the category
            // Articles

            // var dbArticles = await _dbContext.Blog_Articles.Where(x => x.CategoryId == id).ToListAsync();
            var dbArticles = await _dbContext.Blog_Article_Categories.Where(x => x.CategoryId == id).ToListAsync();
            if (dbArticles.Count > 0)
            {
                var dbUncat = await _dbContext.Blog_Categories.Where(x => x.Name == "Uncategorized").FirstOrDefaultAsync();
                Guid unCatId = Guid.Empty;
                if (dbUncat == null)
                {
                    Blog_Category newUncat = new Blog_Category{
                        Id = Guid.NewGuid(),
                        IsEnabled = true,
                        Name = "Uncategorized",
                        LanguageId = await _dbContext.Languages.Where(x => x.IsDefault).Select(x => x.Id).FirstOrDefaultAsync()
                    };
                    await _dbContext.AddAsync(newUncat);
                    await _dbContext.SaveChangesAsync();
                    unCatId = newUncat.Id;
                }
                foreach (var article in dbArticles)
                {
                    article.CategoryId = unCatId;
                    await _dbContext.SaveChangesAsync();
                }
            }

            var dbTags = await _dbContext.Blog_Category_Tags.Where(x => x.CategoryId == id).ToListAsync();
            _dbContext.Blog_Category_Tags.RemoveRange(dbTags);

            _dbContext.Blog_Categories.Remove(dbCategory);
            await _dbContext.SaveChangesAsync();

        }

        public async Task SetParentcategory(rm_sourcetarget data)
        {
            if (data == null) throw new ApiException(HttpStatusCode.NoContent, "");
            if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");

            var dbSourceCategory = await _dbContext.Blog_Categories.FindAsync(data.sourceId);
            var dbTargetCategory = await _dbContext.Blog_Categories.FindAsync(data.targetId);

            if (dbSourceCategory == null || dbTargetCategory == null) throw new ApiException(HttpStatusCode.NotFound, "One of category doesn't exist.");

            dbSourceCategory.ParentCategoryId = dbTargetCategory.Id;
            await _dbContext.SaveChangesAsync();
        }
    }
}