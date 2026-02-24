using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AnasıTAS_Deniz.Business.Infrastructure.Site.Page;
using AnasıTAS_Deniz.Entity.Entities.Site.Page;
using AnasıTAS_Deniz.Entity.Modals.RequestModals.Site.Page;

namespace AnasıTAS_Deniz.API.Controllers.Site
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class PageController : ControllerBase
    {
        private readonly IPageService _pageService;

        public PageController(IPageService pageService)
        {
            _pageService = pageService;
        }

        // GET: api/v1/page/list?lang=tr&status=published
        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPageList([FromQuery] string? lang, [FromQuery] PageStatus? status)
        {
            var pages = await _pageService.GetPagesAsync(lang, status);
            return Ok(pages);
        }

        // GET: api/v1/page/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPageById(Guid id)
        {
            var page = await _pageService.GetPageByIdAsync(id);
            if (page == null)
                return NotFound(new { message = "Page not found" });
            
            return Ok(page);
        }

        // GET: api/v1/page/slug/{slug}?lang=tr
        [HttpGet("slug/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPageBySlug(string slug, [FromQuery] string? lang)
        {
            var page = await _pageService.GetPageBySlugAsync(slug, lang);
            if (page == null)
                return NotFound(new { message = "Page not found" });
            
            return Ok(page);
        }

        // GET: api/v1/page/home?lang=tr
        [HttpGet("home")]
        [AllowAnonymous]
        public async Task<IActionResult> GetHomePage([FromQuery] string? lang)
        {
            var page = await _pageService.GetHomePageAsync(lang);
            if (page == null)
                return NotFound(new { message = "Home page not found" });
            
            return Ok(page);
        }

        // POST: api/v1/page
        [HttpPost]
        public async Task<IActionResult> CreatePage([FromBody] CreatePageRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var page = await _pageService.CreatePageAsync(request);
            return CreatedAtAction(nameof(GetPageById), new { id = page.Id }, page);
        }

        // PUT: api/v1/page/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePage(Guid id, [FromBody] UpdatePageRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _pageService.UpdatePageAsync(id, request);
            if (!updated)
                return NotFound(new { message = "Page not found" });
            
            return Ok(new { message = "Page updated successfully" });
        }

        // POST: api/v1/page/{id}/update
        [HttpPost("{id}/update")]
        public async Task<IActionResult> UpdatePagePost(Guid id, [FromBody] UpdatePageRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var updated = await _pageService.UpdatePageAsync(id, request);
                if (!updated)
                    return NotFound(new { message = "Page not found" });
                
                return Ok(new { message = "Page updated successfully" });
            }
            catch (Exception ex)
            {
                // Log the full exception for debugging
                Console.WriteLine($"Error updating page {id}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                return StatusCode(500, new { 
                    message = "Internal server error while updating page", 
                    error = ex.Message,
                    innerError = ex.InnerException?.Message 
                });
            }
        }

        // DELETE: api/v1/page/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePage(Guid id)
        {
            var deleted = await _pageService.DeletePageAsync(id);
            if (!deleted)
                return NotFound(new { message = "Page not found" });
            
            return Ok(new { message = "Page deleted successfully" });
        }

        // POST: api/v1/page/{id}/delete
        [HttpPost("{id}/delete")]
        public async Task<IActionResult> DeletePagePost(Guid id)
        {
            var deleted = await _pageService.DeletePageAsync(id);
            if (!deleted)
                return NotFound(new { message = "Page not found" });
            
            return Ok(new { message = "Page deleted successfully" });
        }

        // POST: api/v1/page/{id}/publish
        [HttpPost("{id}/publish")]
        public async Task<IActionResult> PublishPage(Guid id)
        {
            var published = await _pageService.PublishPageAsync(id);
            if (!published)
                return NotFound(new { message = "Page not found" });
            
            return Ok(new { message = "Page published successfully" });
        }

        // POST: api/v1/page/{id}/unpublish
        [HttpPost("{id}/unpublish")]
        public async Task<IActionResult> UnpublishPage(Guid id)
        {
            var unpublished = await _pageService.UnpublishPageAsync(id);
            if (!unpublished)
                return NotFound(new { message = "Page not found" });
            
            return Ok(new { message = "Page unpublished successfully" });
        }

        // POST: api/v1/page/{id}/duplicate
        [HttpPost("{id}/duplicate")]
        public async Task<IActionResult> DuplicatePage(Guid id, [FromBody] DuplicatePageRequest? request)
        {
            var duplicatedPage = await _pageService.DuplicatePageAsync(id, request);
            if (duplicatedPage == null)
                return NotFound(new { message = "Page not found" });
            
            return CreatedAtAction(nameof(GetPageById), new { id = duplicatedPage.Id }, duplicatedPage);
        }

        // POST: api/v1/page/{id}/set-home
        [HttpPost("{id}/set-home")]
        public async Task<IActionResult> SetAsHomePage(Guid id)
        {
            var result = await _pageService.SetAsHomePageAsync(id);
            if (!result)
                return NotFound(new { message = "Page not found" });
            
            return Ok(new { message = "Page set as home page successfully" });
        }
    }
}