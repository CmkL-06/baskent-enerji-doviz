using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BaskentEnerji.Business.Infrastructure.Site.Tag;
using BaskentEnerji.Entity.Entities.Site;
using BaskentEnerji.Entity.Modals.RequestModals.Site.Tag;

namespace BaskentEnerji.API.Controllers.Site
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class TagController : ControllerBase
    {
        private readonly ITagServiceCommand _tagServiceCommand;
        private readonly ITagServiceQuery _tagServiceQuery;

        public TagController(ITagServiceCommand tagServiceCommand, ITagServiceQuery tagServiceQuery)
        {
            _tagServiceCommand = tagServiceCommand;
            _tagServiceQuery = tagServiceQuery;
        }


        [HttpGet]
        public List<Entity.Entities.Site.Tag> GetTags()
        {
            return _tagServiceQuery.GetTags();
        }

        [HttpPost]
        public async Task SaveTag(rm_savetag data)
        {
            await _tagServiceCommand.SaveTag(data);
        }

        [HttpPost("article")]
        public async Task AddTag_Article(rm_addtag_article data)
        {
            await _tagServiceCommand.AddTag_Article(data);
        }
        [HttpPost("category")]
        public async Task AddTag_Category(rm_addtag_category data)
        {
            await _tagServiceCommand.AddTag_Category(data);
        }
        [HttpPost("remove")]
        public async Task RemoveTag([FromBody] Guid tagId)
        {
            await _tagServiceCommand.RemoveTag(tagId);
        }
        [HttpPost("remove/article")]
        public async Task RemoveTagFromArticle([FromBody] Guid id)
        {
            await _tagServiceCommand.RemoveTagFromArticle(id);
        }
        [HttpPost("remove/category")]
        public async Task RemoveTagFromCategory(Guid id)
        {
            await _tagServiceCommand.RemoveTagFromCategory(id);
        }

        [HttpGet("article/wcat")]
        [AllowAnonymous]
        public List<Tag> GetTagsByArticleIdWithCategory(Guid ArticleId)
        {
            return _tagServiceQuery.GetTagsByArticleIdWithCategory(ArticleId);
        }
        [HttpGet("article")]
        [AllowAnonymous]
        public List<Tag> GetTagsByArticleIdWithoutCategory(Guid ArticleId)
        {
            return _tagServiceQuery.GetTagsByArticleIdWithoutCategory(ArticleId);
        }

        [HttpGet("category")]
        [AllowAnonymous]
        List<Tag> GetTagsByCategoryId(Guid CategoryId)
        {
            return _tagServiceQuery.GetTagsByCategoryId(CategoryId);
        }

        [HttpGet("language")]
        [AllowAnonymous]
        public List<Tag> GetTagsByLanguageId(Guid LanguageId)
        {
            return _tagServiceQuery.GetTagsByLanguageId(LanguageId);
        }
    }
}
