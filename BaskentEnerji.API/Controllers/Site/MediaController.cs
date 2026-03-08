using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BaskentEnerji.Business.Infrastructure.Site;
using BaskentEnerji.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BaskentEnerji.API.Controllers.Site
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;
        private readonly IWebHostEnvironment _environment;
        private readonly string _uploadPath;

        public MediaController(IMediaService mediaService, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _mediaService = mediaService;
            _environment = environment;
            
            _uploadPath = configuration["MediaSettings:UploadPath"]
                ?? Path.Combine(_environment.ContentRootPath ?? Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetMediaFiles([FromQuery] string type = null, [FromQuery] string search = null)
        {
            try
            {
                var files = await _mediaService.GetAll();
                
                if (!string.IsNullOrEmpty(type))
                {
                    files = files.Where(f => f.FileType == type).ToList();
                }
                
                if (!string.IsNullOrEmpty(search))
                {
                    files = files.Where(f => 
                        f.FileName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        f.OriginalName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        f.Tags?.Contains(search, StringComparison.OrdinalIgnoreCase) == true
                    ).ToList();
                }

                return Ok(files.Select(f => new MediaFileDto
                {
                    Id = f.Id,
                    FileName = f.FileName,
                    OriginalName = f.OriginalName,
                    FileUrl = f.FileUrl,
                    FileType = f.FileType,
                    MimeType = f.MimeType,
                    FileSize = f.FileSize,
                    Width = f.Width,
                    Height = f.Height,
                    AltText = f.AltText,
                    Description = f.Description,
                    Tags = f.Tags,
                    UploadedAt = f.UploadedAt
                }));
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file uploaded" });
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { success = false, message = "File type not allowed" });
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(_uploadPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Determine file type
                var fileType = GetFileType(extension);
                
                // Get image dimensions if it's an image
                int? width = null;
                int? height = null;
                // TODO: Add System.Drawing.Common package to get image dimensions
                // if (fileType == "image")
                // {
                //     using (var image = System.Drawing.Image.FromFile(filePath))
                //     {
                //         width = image.Width;
                //         height = image.Height;
                //     }
                // }

                var uploadedBy = GetCurrentUserId();

                var mediaFile = new MediaFile
                {
                    FileName = fileName,
                    OriginalName = file.FileName,
                    FileUrl = $"/uploads/{fileName}",
                    FileType = fileType,
                    MimeType = file.ContentType,
                    FileSize = file.Length,
                    Width = width,
                    Height = height,
                    FolderPath = "uploads",
                    FilePath = filePath,
                    AltText = "",
                    Title = Path.GetFileNameWithoutExtension(file.FileName),
                    Description = "",
                    Tags = "",
                    UploadedBy = uploadedBy,
                    UploadedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    IsActive = true
                };

                await _mediaService.Add(mediaFile);

                return Ok(new
                {
                    success = true,
                    file = new MediaFileDto
                    {
                        Id = mediaFile.Id,
                        FileName = mediaFile.FileName,
                        OriginalName = mediaFile.OriginalName,
                        FileUrl = mediaFile.FileUrl,
                        FileType = mediaFile.FileType,
                        MimeType = mediaFile.MimeType,
                        FileSize = mediaFile.FileSize,
                        Width = mediaFile.Width,
                        Height = mediaFile.Height,
                        UploadedAt = mediaFile.UploadedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("upload-multiple")]
        public async Task<IActionResult> UploadMultipleFiles(List<IFormFile> files)
        {
            try
            {
                if (files == null || files.Count == 0)
                {
                    return BadRequest(new { success = false, message = "No files uploaded" });
                }

                var uploadedFiles = new List<MediaFileDto>();

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        // Same upload logic as single file
                        var result = await UploadSingleFile(file);
                        if (result != null)
                        {
                            uploadedFiles.Add(result);
                        }
                    }
                }

                return Ok(new { success = true, files = uploadedFiles });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            try
            {
                var mediaFile = await _mediaService.GetById(id);
                if (mediaFile == null)
                {
                    return NotFound(new { success = false, message = "File not found" });
                }

                // Delete physical file
                var webRoot = _environment.WebRootPath ?? _environment.ContentRootPath;
                if (string.IsNullOrEmpty(webRoot))
                {
                    webRoot = Directory.GetCurrentDirectory();
                }
                var wwwrootPath = Path.Combine(webRoot, "wwwroot");
                var filePath = Path.Combine(wwwrootPath, mediaFile.FileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Delete from database
                await _mediaService.Delete(id);

                return Ok(new { success = true, message = "File deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFileDetails(int id, [FromBody] UpdateMediaDto dto)
        {
            try
            {
                var mediaFile = await _mediaService.GetById(id);
                if (mediaFile == null)
                {
                    return NotFound(new { success = false, message = "File not found" });
                }

                mediaFile.AltText = dto.AltText;
                mediaFile.Description = dto.Description;
                mediaFile.Tags = dto.Tags;
                mediaFile.LastModified = DateTime.UtcNow;

                await _mediaService.Update(mediaFile);

                return Ok(new { success = true, message = "File details updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetMediaStatistics()
        {
            try
            {
                var files = await _mediaService.GetAll();
                
                var stats = new
                {
                    totalFiles = files.Count,
                    totalSize = files.Sum(f => f.FileSize),
                    byType = files.GroupBy(f => f.FileType)
                        .Select(g => new { type = g.Key, count = g.Count(), size = g.Sum(f => f.FileSize) }),
                    recentUploads = files.OrderByDescending(f => f.UploadedAt).Take(5)
                        .Select(f => new { f.FileName, f.FileType, f.FileSize, f.UploadedAt })
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private string GetFileType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".svg" => "image",
                ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" => "document",
                ".mp4" or ".avi" or ".mov" or ".wmv" => "video",
                ".mp3" or ".wav" or ".ogg" => "audio",
                _ => "other"
            };
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdClaim, out var userId))
                return userId;
            return 0;
        }

        private async Task<MediaFileDto> UploadSingleFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                return null;
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileType = GetFileType(extension);
            int? width = null;
            int? height = null;
            
            // TODO: Add System.Drawing.Common package to get image dimensions
            // if (fileType == "image")
            // {
            //     using (var image = System.Drawing.Image.FromFile(filePath))
            //     {
            //         width = image.Width;
            //         height = image.Height;
            //     }
            // }

            var mediaFile = new MediaFile
            {
                FileName = fileName,
                OriginalName = file.FileName,
                FileUrl = $"/uploads/{fileName}",
                FileType = fileType,
                MimeType = file.ContentType,
                FileSize = file.Length,
                Width = width,
                Height = height,
                FolderPath = "uploads",
                FilePath = filePath,
                AltText = "",
                Title = Path.GetFileNameWithoutExtension(file.FileName),
                Description = "",
                Tags = "",
                UploadedBy = GetCurrentUserId(),
                UploadedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                IsActive = true
            };

            await _mediaService.Add(mediaFile);

            return new MediaFileDto
            {
                Id = mediaFile.Id,
                FileName = mediaFile.FileName,
                OriginalName = mediaFile.OriginalName,
                FileUrl = mediaFile.FileUrl,
                FileType = mediaFile.FileType,
                MimeType = mediaFile.MimeType,
                FileSize = mediaFile.FileSize,
                Width = mediaFile.Width,
                Height = mediaFile.Height,
                UploadedAt = mediaFile.UploadedAt
            };
        }
    }

    public class MediaFileDto
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string OriginalName { get; set; }
        public string FileUrl { get; set; }
        public string FileType { get; set; }
        public string MimeType { get; set; }
        public long FileSize { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string AltText { get; set; }
        public string Description { get; set; }
        public string Tags { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class UpdateMediaDto
    {
        public string AltText { get; set; }
        public string Description { get; set; }
        public string Tags { get; set; }
    }
}