using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AnasıTAS_Deniz.Business.Infrastructure.Blog.Article;
using AnasıTAS_Deniz.Business.Infrastructure.Blog.Category;
using AnasıTAS_Deniz.Business.Services.Blog;
using AnasıTAS_Deniz.Entity.Modals.RequestModals.Blog;
using AnasıTAS_Deniz.Entity.Modals.RequestModals.General;
using AnasıTAS_Deniz.Entity.Modals.ResponseModals.Blog;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AnasıTAS_Deniz.API.Controllers.Blog
{
    [Route("api/v1/[controller]")]
    [ApiController]
   // [Authorize]
    public class BlogController : ControllerBase
    {
        private readonly IBlog_CategoryServiceCommand _blog_CategoryServiceCommand;
        private readonly IBlog_CategoryServiceQuery _blog_CategoryServiceQuery;
        private readonly IBlog_ArticleServiceCommand _blog_ArticleServiceCommand;
        private readonly IBlog_ArticleServiceQuery _blog_ArticleServiceQuery;
        private readonly HttpContext _httpContext;

        public BlogController(IBlog_CategoryServiceCommand blog_CategoryServiceCommand, IBlog_CategoryServiceQuery blog_CategoryServiceQuery,
            IBlog_ArticleServiceCommand blog_ArticleServiceCommand,
            IBlog_ArticleServiceQuery blog_ArticleServiceQuery
            )
        {
            _blog_CategoryServiceCommand = blog_CategoryServiceCommand;
            _blog_CategoryServiceQuery = blog_CategoryServiceQuery;
            _blog_ArticleServiceCommand = blog_ArticleServiceCommand;
            _blog_ArticleServiceQuery = blog_ArticleServiceQuery;
        }

        [HttpPost("admin/category")]
        public async Task Blog_SaveCategory(rm_savecategory data)
        {
            await _blog_CategoryServiceCommand.SaveCategory(data);
        }


        [HttpPost("admin/category/delete")]
        public async Task Blog_DeleteCategory([FromBody] Guid Id)
        {
            await _blog_CategoryServiceCommand.DeleteCategory(Id);
        }

        [HttpPost("admin/category/setparent")]
        public async Task Blog_SetParentCategory(rm_sourcetarget data)
        {
            await _blog_CategoryServiceCommand.SetParentcategory(data);
        }

        [HttpGet("admin/categorynames")]
        public List<rsp_category_name> getCategoryNames([FromQuery] Guid? langId, Guid? ArticleId, Guid? catId, bool? isEnabled, bool? isUnique, bool? withoutDescription)
        {
            return _blog_CategoryServiceQuery.getCategoryNames(langId, ArticleId, catId, isEnabled, isUnique, withoutDescription);
        }

        [HttpGet("admin/category/id")]
        public rsp_category getCategory(Guid Id, string? link)
        {
            return _blog_CategoryServiceQuery.getCategory(Id, link);
        }

        [HttpGet("admin/category")]
        public async Task<List<rsp_category>> getCategories([FromQuery] rm_category data)
        {
            return await _blog_CategoryServiceQuery.getCategories(data);
        }

        [HttpGet("category")]
        [AllowAnonymous]
        public List<rsp_category_guest> getCategories_Guest([FromQuery] rm_category_guest data)
        {
            return _blog_CategoryServiceQuery.getCategories_Guest(data);
        }

        [HttpPost("admin/article")]
        public async Task SaveArticle(rm_savearticle data)
        {
            await _blog_ArticleServiceCommand.SaveArticle(data);
        }
        [HttpPost("admin/article/addcategory")]
        public async Task AddCategory(List<rm_article_addcategory> data)
        {
            await _blog_ArticleServiceCommand.AddCategory(data);
        }
        [HttpPost("admin/article/delete")]
        public async Task DeleteArticle([FromBody] Guid articleId)
        {
            await _blog_ArticleServiceCommand.DeleteArticle(articleId);
        }

        [HttpGet("admin/article")]
        public rsp_article GetArticle(Guid Id)
        {
            return _blog_ArticleServiceQuery.GetArticle(Id);
        }
        [HttpGet("admin/articles")]
        public List<rsp_article> GetArticles([FromQuery] rm_article data)
        {
            return _blog_ArticleServiceQuery.GetArticles(data);
        }
        [HttpGet("articles")]
        [AllowAnonymous]
        public List<rsp_article_guest> GetArticles_Guest([FromQuery] rm_article_guest data)
        {
            return _blog_ArticleServiceQuery.GetArticles_Guest(data);
        }
        [HttpGet("article")]
        [AllowAnonymous]
        public rsp_article_guest GetArticle_Guest(string link)
        {
            return _blog_ArticleServiceQuery.GetArticle_Guest(link, HttpContext);
        }
        [HttpPost("article/comment")]
        [AllowAnonymous]
        public async Task PostComment(rm_article_comment data)
        {
            await _blog_ArticleServiceCommand.AddComment(data, HttpContext);
        }
    }
}
