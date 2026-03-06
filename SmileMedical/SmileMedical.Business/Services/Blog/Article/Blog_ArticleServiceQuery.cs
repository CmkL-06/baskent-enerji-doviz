using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmileMedical.Business.Exceptions;
using SmileMedical.Business.Infrastructure.Blog.Article;
using SmileMedical.Data.Contexts;
using SmileMedical.Entity.Entities.Site;
using SmileMedical.Entity.Modals.RequestModals.Blog;
using SmileMedical.Entity.Modals.ResponseModals.Blog;
using SmileMedical.Entity.Modals.ResponseModals.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Services.Blog.Article
{
    public class Blog_ArticleServiceQuery : IBlog_ArticleServiceQuery
    {
        private readonly SmileMedicalDbContext _dbContext;
        private readonly IMapper mapper;

        public Blog_ArticleServiceQuery(SmileMedicalDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            this.mapper = mapper;
        }

        public rsp_article GetArticle(Guid Id)
        {
            var dbArticle = _dbContext.Blog_Articles
                .Where(a => a.Id == Id)
                .Select(a => new
                {
                    Article = a,
                    Categories = _dbContext.Blog_Article_Categories
                                    .Where(c => c.ArticleId == a.Id).Include(x => x.Category)
                                    .Select(c => new rsp_category
                                    {
                                        Id = c.CategoryId,
                                        Name = c.Category.Name,
                                        ParentCategoryId = c.Category.ParentCategoryId,
                                        SeoLink = c.Category.SeoLink,
                                        SeoTitle = c.Category.SeoTitle,
                                        Description = c.Category.Description,
                                        ImageUri = c.Category.ImageUri,
                                        IsEnabled = c.Category.IsEnabled,
                                        IsUnique = c.Category.IsUnique,
                                        LanguageId = c.Category.LanguageId,
                                        Order = c.Category.Order,
                                    }).ToList(),
                    Tags = _dbContext.Blog_Article_Tags
                                    .Where(t => t.ArticleId == a.Id).Include(x => x.Tag)
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
                                    }).ToList(),
                    Comments = _dbContext.Blog_Article_Comments
                                     .Where(x => x.ArticleId == a.Id)
                                     .Select(c => mapper.Map<rsp_article_comment>(c)).ToList()
                })
                .FirstOrDefault();

            if (dbArticle == null)
                return null;


            rsp_article rArticle = new rsp_article
            {
                Id = dbArticle.Article.Id,
                Title = dbArticle.Article.Title,
                SeoTitle = dbArticle.Article.SeoTitle,
                SeoLink = dbArticle.Article.SeoLink,
                Description = dbArticle.Article.Description,
                Content = dbArticle.Article.Content,
                AuthorId = dbArticle.Article.AuthorId,
                IsEnabled = dbArticle.Article.IsEnabled,
                IsUnique = dbArticle.Article.IsUnique,
                IsOnSlider = dbArticle.Article.IsOnSlider,
                IsAnnouncement = dbArticle.Article.IsAnnouncement,
                Categories = dbArticle.Categories,
                Tags = dbArticle.Tags,
                CreatedDate = dbArticle.Article.CreatedDate,
                PosterUri = dbArticle.Article.PosterUri,
                PosterHeaderUri = dbArticle.Article.PosterHeaderUri,
                Comments = dbArticle.Comments
            };

            return rArticle;
        }


        public List<rsp_article> GetArticles(rm_article data)
        {
            var query = _dbContext.Blog_Articles.AsQueryable();


            if (data.catId.HasValue)
            {
                query = query.Where(a => _dbContext.Blog_Article_Categories.Any(ac => ac.ArticleId == a.Id && ac.CategoryId == data.catId));
            }

            if (!string.IsNullOrEmpty(data.search)) query = query.Where(x => x.Title.Contains(data.search) || x.SeoTitle.Contains(data.search));
            if (data.isEnabled.HasValue)
                query = query.Where(a => a.IsEnabled == data.isEnabled.Value);

            if (data.isUnique.HasValue)
                query = query.Where(a => a.IsUnique == data.isUnique.Value);
            if (data.isAnnouncement.HasValue)
                query = query.Where(a => a.IsUnique == data.isAnnouncement.Value);

            var result = query.ProjectTo<rsp_article>(mapper.ConfigurationProvider).OrderByDescending(x => x.CreatedDate).ToList();

            if (data.isSimple)
            {
                result.ForEach(item =>
                {
                    item.Content = null;
                    item.Comments = null;
                    item.Description = null;
                });
            }
            return result;
        }


        public List<rsp_article_guest> GetArticles_GuestOld(rm_article_guest data)
        {
            var query = _dbContext.Blog_Articles.AsQueryable();


            if (data.catId.HasValue || data.catId != Guid.Empty)
            {
                query = query.Where(a => _dbContext.Blog_Article_Categories.Any(ac => ac.ArticleId == a.Id && ac.CategoryId == data.catId));
            }

            if (!string.IsNullOrEmpty(data.relatedLink))
            {
                var dbRelatedArticle = _dbContext.Blog_Articles.FirstOrDefault(x => x.SeoLink == data.relatedLink);
                string relatedTitle = dbRelatedArticle?.SeoLink ?? data.relatedLink;
                string[] relatedWords = relatedTitle.Split('-');
                var dbArticles = _dbContext.Blog_Articles
                    .Where(x => relatedWords.Any(word => x.SeoTitle.Contains(word))).ToList();
                return mapper.Map<List<rsp_article_guest>>(dbArticles);
            }



            if (data.isEnabled.HasValue)
                query = query.Where(a => a.IsEnabled == data.isEnabled.Value);

            if (data.isUnique.HasValue)
                query = query.Where(a => a.IsUnique == data.isUnique.Value);
            if (data.isAnnouncement.HasValue)
                query = query.Where(a => a.IsAnnouncement == data.isAnnouncement.Value);

            var result = query.ProjectTo<rsp_article_guest>(mapper.ConfigurationProvider).OrderByDescending(x => x.CreatedDate).ToList();



            return result;
        }
        public List<rsp_article_guest> GetArticles_Guest(rm_article_guest data)
        {
            var query = _dbContext.Blog_Articles.AsQueryable();

            query = query.Include(x => x.Author)
                         .Include(x => x.Blog_Article_Tags)
                         .Include(x => x.Blog_Article_Categories)
                         .ThenInclude(c => c.Category);

            if (!string.IsNullOrEmpty(data.relatedLink))
            {
                string relatedTitle;
                string langCode= null;
              
                var dbRelatedArticle = _dbContext.Blog_Articles.Where(x => x.SeoLink == data.relatedLink).Include(x => x.Blog_Article_Categories).ThenInclude(c => c.Category.Language).FirstOrDefault();

                if (dbRelatedArticle == null) relatedTitle = data.relatedLink;
                else langCode = dbRelatedArticle.Blog_Article_Categories.Select(x => x.Category.Language.LanguageCode).FirstOrDefault();


                if (!string.IsNullOrEmpty(data.Lang)) langCode = data.Lang;
               

                relatedTitle = dbRelatedArticle?.SeoLink ?? data.relatedLink;
                
                string[] relatedWords = relatedTitle.Split('-');

               if (string.IsNullOrEmpty(langCode))
                {
                    var dbArticles = _dbContext.Blog_Articles
                    .Where(x => relatedWords.Any(word => x.SeoTitle.Contains(word))).OrderByDescending(x=> x.CreatedDate).ToList();
                    return mapper.Map<List<rsp_article_guest>>(dbArticles);
                } 
                else
                {
                    var dbArticles = _dbContext.Blog_Articles
                  .Where(x => x.Blog_Article_Categories.Any(c => c.Category.Language.LanguageCode == langCode) && relatedWords.Any(word => x.SeoTitle.Contains(word))).ToList();
                    return mapper.Map<List<rsp_article_guest>>(dbArticles.OrderByDescending(x=> x.CreatedDate));
                }

               
            }

            if (!string.IsNullOrEmpty(data.Lang)) query = query.Where(x => x.Blog_Article_Categories.Any(c => c.Category.Language.LanguageCode == data.Lang));
          
            if (data.catId.HasValue) query = query.Where(x => x.Blog_Article_Categories.Any(ac => ac.CategoryId == data.catId));
            if (string.IsNullOrEmpty(data.catLink)) query = query.Where(x => x.Blog_Article_Categories.Any(ac => ac.Category.SeoLink == data.catLink));
          

            if (data.isEnabled.HasValue)
            {
                query = query.Where(x => x.IsEnabled == data.isEnabled.Value);
            }

            if (data.isUnique.HasValue)
            {
                query = query.Where(x => x.IsUnique == data.isUnique.Value);
            }

            if (data.isAnnouncement.HasValue)
            {
                query = query.Where(x => x.IsAnnouncement == data.isAnnouncement.Value);
            }

            var result = query.ProjectTo<rsp_article_guest>(mapper.ConfigurationProvider)
                              .OrderByDescending(x => x.CreatedDate)
                              .ToList();
            if (data.isSimple)
            {
                result.ForEach(item =>
                {
                    item.Content = null;
                    item.Comments = null;
                    item.Description = null;
                });
            }

            return result;

        }

        public rsp_article_guest GetArticle_Guest(string seoLink, HttpContext httpContext)
        {
            //   var dbArticle = _dbContext.Blog_Articles.FirstOrDefault(x => x.SeoLink == seoLink);
            //  if (dbArticle == null) throw new ApiException(HttpStatusCode.NotFound, "Article couldn't be found");
            //  return mapper.Map<rsp_article_guest>(dbArticle);


            var dbArticle = _dbContext.Blog_Articles
                 .Where(a => a.SeoLink == seoLink)
                 .Select(a => new
                 {
                     Article = a,
                     Categories = _dbContext.Blog_Article_Categories
                                     .Where(c => c.ArticleId == a.Id).Include(x => x.Category)
                                     .Select(c => new rsp_category_guest
                                     {

                                         Name = c.Category.Name,

                                         SeoLink = c.Category.SeoLink,
                                         SeoTitle = c.Category.SeoTitle,
                                         Description = c.Category.Description,
                                         ImageUri = c.Category.ImageUri,



                                     }).ToList(),
                     Tags = _dbContext.Blog_Article_Tags
                                     .Where(t => t.ArticleId == a.Id).Include(x => x.Tag)
                                     .Select(t => new rsp_tag_guest
                                     {


                                         SeoLink = t.Tag.SeoLink,

                                         Name = t.Tag.Name,
                                     }).ToList(),

                     Comments = _dbContext.Blog_Article_Comments
                                     .Where(x => x.ArticleId == a.Id)
                                     .Select(c => mapper.Map<rsp_article_comment>(c)).ToList()
                 })
                 .FirstOrDefault();

            if (dbArticle == null)
                return null;


            rsp_article_guest rArticle = new rsp_article_guest
            {
                Id = dbArticle.Article.Id,
                Title = dbArticle.Article.Title,
                SeoTitle = dbArticle.Article.SeoTitle,
                SeoLink = dbArticle.Article.SeoLink,
                Description = dbArticle.Article.Description,
                Content = dbArticle.Article.Content,
                Views = dbArticle.Article.Views,
                IsAnnouncement = dbArticle.Article.IsAnnouncement,
                Categories = dbArticle.Categories,
                Tags = dbArticle.Tags,
                CreatedDate = dbArticle.Article.CreatedDate,
                PosterUri = dbArticle.Article.PosterUri,
                PosterHeaderUri = dbArticle.Article.PosterHeaderUri,
                Comments = dbArticle.Comments,
            };


            var visitorIp = Tools.tools_string.GetIpAddress(httpContext);
            var checkAlreadyViewed = _dbContext.Blog_Article_Visits.Where(x => x.VisitorIp == visitorIp).ToList();
            if (checkAlreadyViewed.Count == 0)
            {
                _dbContext.Blog_Article_Visits.Add(new Entity.Entities.Blog.Blog_Article_Visit
                {
                    ArticleId = dbArticle.Article.Id,
                    VisitorIp = visitorIp,
                });
                dbArticle.Article.Views++;
                _dbContext.SaveChanges();
                rArticle.Views++;
            }


            return rArticle;
        }
    }
}
