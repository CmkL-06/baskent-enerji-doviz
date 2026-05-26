using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BaskentEnerji.Business.Infrastructure.Blog.Article;
using BaskentEnerji.Business.Infrastructure.Blog.Category;
using BaskentEnerji.Business.Services.Blog;
using BaskentEnerji.Entity.Modals.RequestModals.Blog;
using BaskentEnerji.Entity.Modals.RequestModals.General;
using BaskentEnerji.Entity.Modals.ResponseModals.Blog;

namespace BaskentEnerji.API.Controllers.Blog
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
        public async Task<IActionResult> Blog_SaveCategory(rm_savecategory data)
        {
            try
            {
                await _blog_CategoryServiceCommand.SaveCategory(data);
                return Ok(new { success = true, message = "Kategori başarıyla kaydedildi" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "İç sunucu hatası", error = ex.Message });
            }
        }

        [HttpPost("admin/category/delete")]
        public async Task<IActionResult> Blog_DeleteCategory([FromBody] Guid Id)
        {
            try
            {
                await _blog_CategoryServiceCommand.DeleteCategory(Id);
                return Ok(new { success = true, message = "Kategori başarıyla silindi" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "İç sunucu hatası", error = ex.Message });
            }
        }

        [HttpPost("admin/category/setparent")]
        public async Task<IActionResult> Blog_SetParentCategory(rm_sourcetarget data)
        {
            try
            {
                await _blog_CategoryServiceCommand.SetParentcategory(data);
                return Ok(new { success = true, message = "Üst kategori başarıyla ayarlandı" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "İç sunucu hatası", error = ex.Message });
            }
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
        public async Task<IActionResult> SaveArticle(rm_savearticle data)
        {
            try
            {
                await _blog_ArticleServiceCommand.SaveArticle(data);
                return Ok(new { success = true, message = "Makale başarıyla kaydedildi" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "İç sunucu hatası", error = ex.Message });
            }
        }

        [HttpPost("admin/article/addcategory")]
        public async Task<IActionResult> AddCategory(List<rm_article_addcategory> data)
        {
            try
            {
                await _blog_ArticleServiceCommand.AddCategory(data);
                return Ok(new { success = true, message = "Kategori başarıyla eklendi" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "İç sunucu hatası", error = ex.Message });
            }
        }

        [HttpPost("admin/article/delete")]
        public async Task<IActionResult> DeleteArticle([FromBody] Guid articleId)
        {
            try
            {
                await _blog_ArticleServiceCommand.DeleteArticle(articleId);
                return Ok(new { success = true, message = "Makale başarıyla silindi" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "İç sunucu hatası", error = ex.Message });
            }
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
        public async Task<IActionResult> PostComment(rm_article_comment data)
        {
            try
            {
                await _blog_ArticleServiceCommand.AddComment(data, HttpContext);
                return Ok(new { success = true, message = "Yorum başarıyla eklendi" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "İç sunucu hatası", error = ex.Message });
            }
        }
    }
}
