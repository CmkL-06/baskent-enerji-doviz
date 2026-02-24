using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using AnasıTAS_Deniz.Data.Contexts;
using AnasıTAS_Deniz.Entity.Entities.Site;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.API.Controllers.Site
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class GlobalColorController : ControllerBase
    {
        private readonly AnasıTAS_DenizDbContext _context;

        public GlobalColorController(AnasıTAS_DenizDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get active global colors
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveColors()
        {
            try
            {
                var activeTheme = await _context.GlobalColors
                    .Where(x => x.IsActive)
                    .OrderByDescending(x => x.UpdatedAt)
                    .FirstOrDefaultAsync();

                if (activeTheme == null)
                {
                    return NotFound(new { message = "No active theme found" });
                }

                var response = new
                {
                    id = activeTheme.Id,
                    name = activeTheme.Name,
                    colors = JsonConvert.DeserializeObject(activeTheme.ColorsJson),
                    isActive = activeTheme.IsActive,
                    createdAt = activeTheme.CreatedAt,
                    updatedAt = activeTheme.UpdatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error getting active colors", error = ex.Message });
            }
        }

        /// <summary>
        /// Create new global color theme
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateColorTheme([FromBody] CreateColorThemeDto dto)
        {
            try
            {
                // Deactivate all existing themes
                var existingThemes = await _context.GlobalColors.Where(x => x.IsActive).ToListAsync();
                foreach (var theme in existingThemes)
                {
                    theme.IsActive = false;
                }

                // Create new theme
                var newTheme = new GlobalColor
                {
                    Name = dto.Name ?? "Default Theme",
                    ColorsJson = JsonConvert.SerializeObject(dto.Colors),
                    IsActive = dto.IsActive ?? true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.GlobalColors.Add(newTheme);
                await _context.SaveChangesAsync();

                var response = new
                {
                    id = newTheme.Id,
                    name = newTheme.Name,
                    colors = dto.Colors,
                    isActive = newTheme.IsActive,
                    createdAt = newTheme.CreatedAt,
                    updatedAt = newTheme.UpdatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating color theme", error = ex.Message });
            }
        }

        /// <summary>
        /// Update existing global color theme
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateColorTheme(int id, [FromBody] UpdateColorThemeDto dto)
        {
            try
            {
                var theme = await _context.GlobalColors.FindAsync(id);
                if (theme == null)
                {
                    return NotFound(new { message = "Theme not found" });
                }

                theme.ColorsJson = JsonConvert.SerializeObject(dto.Colors);
                theme.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var response = new
                {
                    id = theme.Id,
                    name = theme.Name,
                    colors = dto.Colors,
                    isActive = theme.IsActive,
                    createdAt = theme.CreatedAt,
                    updatedAt = theme.UpdatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating color theme", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete global color theme
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteColorTheme(int id)
        {
            try
            {
                var theme = await _context.GlobalColors.FindAsync(id);
                if (theme == null)
                {
                    return NotFound(new { message = "Theme not found" });
                }

                if (theme.IsActive)
                {
                    return BadRequest(new { message = "Cannot delete active theme" });
                }

                _context.GlobalColors.Remove(theme);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Theme deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting color theme", error = ex.Message });
            }
        }
    }

    public class CreateColorThemeDto
    {
        public string Name { get; set; }
        public ColorSchemeDto Colors { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UpdateColorThemeDto
    {
        public ColorSchemeDto Colors { get; set; }
    }

    public class ColorSchemeDto
    {
        public string Primary { get; set; }
        public string PrimaryHover { get; set; }
        public string Secondary { get; set; }
        public string SecondaryHover { get; set; }
        public string Accent { get; set; }
        public string Success { get; set; }
        public string Warning { get; set; }
        public string Danger { get; set; }
        public string Info { get; set; }
        public string Dark { get; set; }
        public string Light { get; set; }
        public string Muted { get; set; }
    }
}