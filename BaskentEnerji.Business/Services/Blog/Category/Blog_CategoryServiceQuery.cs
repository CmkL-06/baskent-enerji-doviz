using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BaskentEnerji.Business.Exceptions;
using BaskentEnerji.Business.Infrastructure.Blog.Category;
using BaskentEnerji.Business.Services.Permission;
using BaskentEnerji.Business.Tools;
using BaskentEnerji.Data.Contexts;
using BaskentEnerji.Entity.Entities.Site;
using BaskentEnerji.Entity.Modals.RequestModals.Blog;
using BaskentEnerji.Entity.Modals.ResponseModals.Blog;
using BaskentEnerji.Entity.Modals.ResponseModals.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Services.Blog.Category
{
    public class Blog_CategoryServiceQuery : IBlog_CategoryServiceQuery
    {
        private readonly BaskentEnerjiDbContext _dbContext;
        private readonly ValidationService _validationService;
        private readonly IMapper mapper;
        private readonly ILogger<Blog_CategoryServiceQuery> _logger;

        public Blog_CategoryServiceQuery(BaskentEnerjiDbContext dbContext, ValidationService validationService, IMapper mapper, ILogger<Blog_CategoryServiceQuery> logger)
        {
            _dbContext = dbContext;
            _validationService = validationService;
            this.mapper = mapper;
            _logger = logger;
        }
        public async Task<List<rsp_category>> getCategories(rm_category data)
        {
              if (!await _validationService.IsStaff()) throw new ApiException(HttpStatusCode.Unauthorized, "You have no permission to do this.");



            if (data.ArticleId.HasValue)
            {
                return _dbContext.Blog_Article_Categories
                     .Where(x => x.ArticleId == data.ArticleId)
                     .Select(x => x.Category)
                     .ProjectTo<rsp_category>(mapper.ConfigurationProvider)
                     .ToList();
            }

            var query = _dbContext.Blog_Categories.AsQueryable();
            if (!string.IsNullOrEmpty(data.search)) query = query.Where(x => x.Name.Contains(data.search) || x.SeoTitle.Contains(data.search));
            if (data.langId.HasValue) query = query.Where(x => x.LanguageId == data.langId.Value);
            if (data.catId.HasValue) query = query.Where(x => x.Id == data.catId.Value);
            if (data.isEnabled.HasValue) query = query.Where(x => x.IsEnabled == data.isEnabled.Value);
            if (data.isUnique.HasValue) query = query.Where(x => x.IsUnique == data.isUnique.Value);
            if (data.withoutDescription.HasValue && data.withoutDescription.Value) query = query.Where(x => string.IsNullOrEmpty(x.Description));
            if (data.isMain.HasValue) query = query.Where(x => x.ParentCategory == null);
            var result = query.OrderByDescending(x=> x.CreatedDate).ToList();
            var mappedResult = result.Select(x => mapper.Map<rsp_category>(x)).ToList();
            return mappedResult;
        }

        public List<rsp_category_guest> getCategories_Guest(rm_category_guest data)
        {

            if (data.ArticleId.HasValue && data.ArticleId != Guid.Empty)
            {
                var articleCategories = _dbContext.Blog_Article_Categories
                    .Where(x => x.ArticleId == data.ArticleId)
                    .Select(x => x.Category)
                    .Include(x => x.Language)
                    .ToList();

                var projectedCategories = articleCategories
                    .AsQueryable().OrderBy(x => x.Order)
                    .ProjectTo<rsp_category_guest>(mapper.ConfigurationProvider)
                    .ToList();

                return projectedCategories;
            }

            var query = _dbContext.Blog_Categories.Include(x => x.Language).Include(x => x.ParentCategory).AsQueryable();

            if (data.langId.HasValue) query = query.Where(x => x.LanguageId == data.langId.Value);
            if (data.catId.HasValue) query = query.Where(x => x.Id == data.catId.Value);
            if (data.isEnabled.HasValue) query = query.Where(x => x.IsEnabled == data.isEnabled.Value);
            if (data.isUnique.HasValue) query = query.Where(x => x.IsUnique == data.isUnique.Value);
            if (!string.IsNullOrEmpty(data.link)) query = query.Where(x => x.SeoLink == data.link);
            if (!string.IsNullOrEmpty(data.parentLink)) query = query.Where(x => x.ParentCategory.SeoLink == data.parentLink);

            var result = query.OrderBy(x => x.Order).ToList();

            // Log the result count
            _logger.LogInformation($"Categories found: {result.Count}");

            var mappedResult = result.Select(x => mapper.Map<rsp_category_guest>(x)).OrderBy(x=> x.Order).ToList();

            if (data.isSimple)
            {
                mappedResult.ForEach(item =>
                {
                    item.Content = null;
                    item.Description = null;
                    item.PosterHeaderUri = null;
                    item.Id = Guid.Empty;
                    item.ParentCategoryName = null;
                });
            }
            mappedResult.ForEach(item =>
            {
                var dbTags = _dbContext.Blog_Category_Tags
                                    .Where(t => t.CategoryId == item.Id).Include(x => x.Tag)
                                    .Select(x => x.Tag).ToList();
                item.Tags = mapper.Map<List<rsp_tag_guest>>(dbTags);
            });

            if (!string.IsNullOrEmpty(data.parentLink) && mappedResult.Count == 0)
            {
                var dbCat = _dbContext.Blog_Categories.FirstOrDefault(x => x.SeoLink == data.parentLink);
                if (dbCat.ParentCategoryId.HasValue)
                {
                    var dbParents = _dbContext.Blog_Categories.Where(x => x.ParentCategoryId == dbCat.ParentCategoryId).OrderBy(x => x.Order).ToList();
                    mappedResult = mapper.Map<List<rsp_category_guest>>(dbParents);
                }
            }
                                    

            return mappedResult;

        }

        public rsp_category getCategory(Guid catId, string? link)
        {
            var dbCategory = _dbContext.Blog_Categories
                 .Where(a => !string.IsNullOrEmpty(link) ? a.SeoLink == link : a.Id == catId)
                 .Include(x => x.Language)
                 .Include(x => x.ParentCategory)
                 
                 .Select(a => new
                 {
                     Category = a,

                     ParentCategory = a.ParentCategory != null ? new rsp_category
                     {
                         Id = a.ParentCategory.Id,
                         Name = a.ParentCategory.Name,
                         ParentCategoryId = a.ParentCategory.Id,
                         SeoLink = a.ParentCategory.SeoLink,
                         SeoTitle = a.ParentCategory.SeoTitle,
                         Description = a.ParentCategory.Description,
                         ImageUri = a.ParentCategory.ImageUri,
                         IsEnabled = a.ParentCategory.IsEnabled,
                         IsUnique = a.ParentCategory.IsUnique,
                         LanguageId = a.ParentCategory.LanguageId,
                         Order = a.ParentCategory.Order,
                         PosterHeaderUri = a.ParentCategory.PosterHeaderUri,
                         PosterUri = a.ParentCategory.PosterUri,
                         LanguageCode = a.ParentCategory.Language.LanguageCode,
                         LanguageName = a.ParentCategory.Language.LanguageName,
                         IsDisplayPage = a.ParentCategory.IsDisplayPage,
                         PageId = a.ParentCategory.PageId,
                         PageTitle = tools_string.DeGenerateSlug(a.ParentCategory.PageId),
                     } : null,

                     Tags = _dbContext.Blog_Category_Tags
                                     .Where(t => t.CategoryId == catId).Include(x => x.Tag)
                                     .Select(t => new Tag
                                     {
                                         Id = t.TagId,
                                         Content = t.Tag.Content,
                                         SeoLink = t.Tag.SeoLink,
                                         SeoTitle = t.Tag.SeoTitle,
                                         ArticleTags = t.Tag.ArticleTags,
                                         CategoryTags = t.Tag.CategoryTags,
                                         CreatedDate = t.Tag.CreatedDate,
                                         LanguageId = t.Tag.LanguageId,
                                         Language = t.Tag.Language,
                                         Name = t.Tag.Name,
                                     }).ToList()

                 })
                 .FirstOrDefault();

            if (dbCategory == null)
                return null;


            rsp_category rCategory = new rsp_category
            {
                Id = dbCategory.Category.Id,
                Name = dbCategory.Category.Name,
                SeoTitle = dbCategory.Category.SeoTitle,
                SeoLink = dbCategory.Category.SeoLink,
                Description = dbCategory.Category.Description,
                Content = dbCategory.Category.Content,
                IsEnabled = dbCategory.Category.IsEnabled,
                IsUnique = dbCategory.Category.IsUnique,
                ParentCategory = dbCategory.ParentCategory,
                Tags = dbCategory.Tags,
                CreatedDate = dbCategory.Category.CreatedDate,
                PosterUri = dbCategory.Category.PosterUri,
                PosterHeaderUri = dbCategory.Category.PosterHeaderUri,
                ImageUri = dbCategory.Category.ImageUri,
                LanguageCode = dbCategory.Category.Language.LanguageCode,
                LanguageName = dbCategory.Category.Language.LanguageName,
                LanguageId = dbCategory.Category.Language.Id,
                ParentCategoryId = dbCategory.ParentCategory?.Id,
                Order = dbCategory.Category.Order,
                IsDisplayPage = dbCategory.Category.IsDisplayPage,
                PageId = dbCategory.Category.PageId,
            };

            return rCategory;
        }

        public List<rsp_category_name> getCategoryNames(Guid? langId, Guid? ArticleId, Guid? catId, bool? isEnabled, bool? isUnique, bool? withoutDescription)
        {
            if (ArticleId.HasValue)
            {

                return _dbContext.Blog_Article_Categories
                     .Where(x => x.ArticleId == ArticleId)
                       .Include(x => x.Category)
                     .Select(x => mapper.Map<rsp_category_name>(x))
                     .ToList();
            }

            var query = _dbContext.Blog_Categories.Include(x => x.Language).Include(x => x.ParentCategory).AsQueryable();

            if (langId.HasValue) query = query.Where(x => x.LanguageId == langId.Value);
            if (catId.HasValue) query = query.Where(x => x.Id == catId.Value);
            if (isEnabled.HasValue) query = query.Where(x => x.IsEnabled == isEnabled.Value);
            if (isUnique.HasValue) query = query.Where(x => x.IsUnique == isUnique.Value);
            if (withoutDescription.HasValue && withoutDescription.Value) query = query.Where(x => string.IsNullOrEmpty(x.Description));

            var result = query.ToList();
            return result.Select(x => mapper.Map<rsp_category_name>(x)).ToList();

        }
    }
}
