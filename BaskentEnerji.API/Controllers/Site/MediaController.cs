using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BaskentEnerji.Business.Infrastructure.Site;
using BaskentEnerji.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
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
                if (fileType == "image" && TryGetImageDimensions(filePath, extension, out var detectedWidth, out var detectedHeight))
                {
                    width = detectedWidth;
                    height = detectedHeight;
                }

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

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private static bool TryGetImageDimensions(string filePath, string extension, out int? width, out int? height)
        {
            width = null;
            height = null;

            try
            {
                using var stream = File.OpenRead(filePath);
                return extension.ToLowerInvariant() switch
                {
                    ".png" => TryReadPngDimensions(stream, out width, out height),
                    ".gif" => TryReadGifDimensions(stream, out width, out height),
                    ".jpg" or ".jpeg" => TryReadJpegDimensions(stream, out width, out height),
                    ".webp" => TryReadWebpDimensions(stream, out width, out height),
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReadPngDimensions(Stream stream, out int? width, out int? height)
        {
            width = null;
            height = null;

            var buffer = new byte[24];
            if (!TryReadExactly(stream, buffer, buffer.Length))
            {
                return false;
            }

            if (buffer[0] != 0x89 || buffer[1] != 0x50 || buffer[2] != 0x4E || buffer[3] != 0x47)
            {
                return false;
            }

            width = (buffer[16] << 24) | (buffer[17] << 16) | (buffer[18] << 8) | buffer[19];
            height = (buffer[20] << 24) | (buffer[21] << 16) | (buffer[22] << 8) | buffer[23];
            return true;
        }

        private static bool TryReadGifDimensions(Stream stream, out int? width, out int? height)
        {
            width = null;
            height = null;

            var buffer = new byte[10];
            if (!TryReadExactly(stream, buffer, buffer.Length))
            {
                return false;
            }

            if (buffer[0] != 0x47 || buffer[1] != 0x49 || buffer[2] != 0x46)
            {
                return false;
            }

            width = buffer[6] | (buffer[7] << 8);
            height = buffer[8] | (buffer[9] << 8);
            return true;
        }

        private static bool TryReadWebpDimensions(Stream stream, out int? width, out int? height)
        {
            width = null;
            height = null;

            var header = new byte[30];
            if (!TryReadExactly(stream, header, header.Length))
            {
                return false;
            }

            if (header[0] != 0x52 || header[1] != 0x49 || header[2] != 0x46 || header[3] != 0x46 ||
                header[8] != 0x57 || header[9] != 0x45 || header[10] != 0x42 || header[11] != 0x50)
            {
                return false;
            }

            var chunk = System.Text.Encoding.ASCII.GetString(header, 12, 4);
            if (chunk == "VP8X")
            {
                width = 1 + (header[24] | (header[25] << 8) | (header[26] << 16));
                height = 1 + (header[27] | (header[28] << 8) | (header[29] << 16));
                return true;
            }

            if (chunk == "VP8 ")
            {
                width = header[26] | ((header[27] & 0x3F) << 8);
                height = header[28] | ((header[29] & 0x3F) << 8);
                return true;
            }

            if (chunk == "VP8L")
            {
                var b0 = header[21];
                var b1 = header[22];
                var b2 = header[23];
                var b3 = header[24];
                width = 1 + (((b1 & 0x3F) << 8) | b0);
                height = 1 + (((b3 & 0x0F) << 10) | (b2 << 2) | ((b1 & 0xC0) >> 6));
                return true;
            }

            return false;
        }

        private static bool TryReadJpegDimensions(Stream stream, out int? width, out int? height)
        {
            width = null;
            height = null;

            if (stream.ReadByte() != 0xFF || stream.ReadByte() != 0xD8)
            {
                return false;
            }

            while (stream.Position < stream.Length)
            {
                var markerStart = stream.ReadByte();
                if (markerStart != 0xFF)
                {
                    continue;
                }

                var marker = stream.ReadByte();
                while (marker == 0xFF)
                {
                    marker = stream.ReadByte();
                }

                if (marker == -1)
                {
                    return false;
                }

                if (marker is 0xD8 or 0xD9)
                {
                    continue;
                }

                var lh = stream.ReadByte();
                var ll = stream.ReadByte();
                if (lh == -1 || ll == -1)
                {
                    return false;
                }

                var segmentLength = (lh << 8) + ll;
                if (segmentLength < 2)
                {
                    return false;
                }

                if (marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF)
                {
                    if (stream.ReadByte() == -1)
                    {
                        return false;
                    }

                    var h1 = stream.ReadByte();
                    var h2 = stream.ReadByte();
                    var w1 = stream.ReadByte();
                    var w2 = stream.ReadByte();
                    if (h1 == -1 || h2 == -1 || w1 == -1 || w2 == -1)
                    {
                        return false;
                    }

                    height = (h1 << 8) + h2;
                    width = (w1 << 8) + w2;
                    return true;
                }

                stream.Seek(segmentLength - 2, SeekOrigin.Current);
            }

            return false;
        }

        private static bool TryReadExactly(Stream stream, byte[] buffer, int count)
        {
            var totalRead = 0;
            while (totalRead < count)
            {
                var read = stream.Read(buffer, totalRead, count - totalRead);
                if (read == 0)
                {
                    return false;
                }

                totalRead += read;
            }

            return true;
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

            if (fileType == "image" && TryGetImageDimensions(filePath, extension, out var detectedWidth, out var detectedHeight))
            {
                width = detectedWidth;
                height = detectedHeight;
            }

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