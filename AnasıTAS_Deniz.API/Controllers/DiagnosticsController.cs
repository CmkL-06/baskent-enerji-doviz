using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnasıTAS_Deniz.Data.Contexts;

namespace AnasıTAS_Deniz.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class DiagnosticsController : ControllerBase
    {
        private readonly AnasıTAS_DenizDbContext _db;

        public DiagnosticsController(AnasıTAS_DenizDbContext db)
        {
            _db = db;
        }

        [HttpGet("db")]
        public async Task<IActionResult> TestDb()
        {
            try
            {
                var canConnect = await _db.Database.CanConnectAsync();
                var userCount = await _db.Users.CountAsync();
                return Ok(new { Ok = true, CanConnect = canConnect, UserCount = userCount });
            }
            catch (Exception ex)
            {
                var msg = (ex.InnerException?.Message ?? ex.Message) ?? "?";
                return Ok(new { Ok = false, Error = msg });
            }
        }
    }
}
