using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoneyTransferTurkey.Business.Infrastructure.Site;
using MoneyTransferTurkey.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.API.Controllers.Site
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
            
            // Handle null WebRootPath (common in development)
            var webRoot = _environment.WebRootPath ?? _environment.ContentRootPath;
            if (string.IsNullOrEmpty(webRoot))
            {
                // Fallback to current directory
                webRoot = Directory.GetCurrentDirectory();
            }
            
            _uploadPath = configuration["MediaSettings:UploadPath"]
                ?? Path.Combine(webRoot, "wwwroot", "uploads");
            
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

                // Save to database
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
                    AltText = "", // Required field, cannot be null
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

        private static bool TryGetImageDimensions(string filePath, string extension, out int? width, out int? height)
        {
            width = null;
            height = null;

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return extension.ToLowerInvariant() switch
                {
                    ".png" => TryReadPngDimensions(stream, out width, out height),
                    ".jpg" or ".jpeg" => TryReadJpegDimensions(stream, out width, out height),
                    ".gif" => TryReadGifDimensions(stream, out width, out height),
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
                return false;

            var isPng = buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 &&
                        buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A;
            var isIhdr = buffer[12] == 0x49 && buffer[13] == 0x48 && buffer[14] == 0x44 && buffer[15] == 0x52;
            if (!isPng || !isIhdr)
                return false;

            width = (buffer[16] << 24) | (buffer[17] << 16) | (buffer[18] << 8) | buffer[19];
            height = (buffer[20] << 24) | (buffer[21] << 16) | (buffer[22] << 8) | buffer[23];
            return width > 0 && height > 0;
        }

        private static bool TryReadGifDimensions(Stream stream, out int? width, out int? height)
        {
            width = null;
            height = null;

            var buffer = new byte[10];
            if (!TryReadExactly(stream, buffer, buffer.Length))
                return false;

            var isGif = buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46;
            if (!isGif)
                return false;

            width = buffer[6] | (buffer[7] << 8);
            height = buffer[8] | (buffer[9] << 8);
            return width > 0 && height > 0;
        }

        private static bool TryReadWebpDimensions(Stream stream, out int? width, out int? height)
        {
            width = null;
            height = null;

            var header = new byte[12];
            if (!TryReadExactly(stream, header, header.Length))
                return false;

            var isRiff = header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46;
            var isWebp = header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;
            if (!isRiff || !isWebp)
                return false;

            while (stream.Position + 8 <= stream.Length)
            {
                var chunkHeader = new byte[8];
                if (!TryReadExactly(stream, chunkHeader, chunkHeader.Length))
                    return false;

                var chunkSize = chunkHeader[4] | (chunkHeader[5] << 8) | (chunkHeader[6] << 16) | (chunkHeader[7] << 24);
                if (chunkSize < 0)
                    return false;

                var isVp8X = chunkHeader[0] == 0x56 && chunkHeader[1] == 0x50 && chunkHeader[2] == 0x38 && chunkHeader[3] == 0x58;
                if (isVp8X)
                {
                    var payload = new byte[10];
                    if (!TryReadExactly(stream, payload, payload.Length))
                        return false;
                    width = 1 + payload[4] + (payload[5] << 8) + (payload[6] << 16);
                    height = 1 + payload[7] + (payload[8] << 8) + (payload[9] << 16);
                    return true;
                }

                var isVp8L = chunkHeader[0] == 0x56 && chunkHeader[1] == 0x50 && chunkHeader[2] == 0x38 && chunkHeader[3] == 0x4C;
                if (isVp8L)
                {
                    var payload = new byte[5];
                    if (!TryReadExactly(stream, payload, payload.Length) || payload[0] != 0x2F)
                        return false;

                    width = 1 + (((payload[1] | (payload[2] << 8)) & 0x3FFF));
                    height = 1 + ((((payload[2] >> 6) | (payload[3] << 2) | (payload[4] << 10)) & 0x3FFF));
                    return true;
                }

                var isVp8 = chunkHeader[0] == 0x56 && chunkHeader[1] == 0x50 && chunkHeader[2] == 0x38 && chunkHeader[3] == 0x20;
                if (isVp8)
                {
                    var payload = new byte[10];
                    if (!TryReadExactly(stream, payload, payload.Length))
                        return false;

                    width = (payload[6] | (payload[7] << 8)) & 0x3FFF;
                    height = (payload[8] | (payload[9] << 8)) & 0x3FFF;
                    return width > 0 && height > 0;
                }

                var bytesToSkip = chunkSize + (chunkSize % 2);
                if (stream.Position + bytesToSkip > stream.Length)
                    return false;
                stream.Seek(bytesToSkip, SeekOrigin.Current);
            }

            return false;
        }

        private static bool TryReadJpegDimensions(Stream stream, out int? width, out int? height)
        {
            width = null;
            height = null;

            if (stream.ReadByte() != 0xFF || stream.ReadByte() != 0xD8)
                return false;

            while (stream.Position < stream.Length)
            {
                var markerPrefix = stream.ReadByte();
                if (markerPrefix < 0)
                    return false;
                if (markerPrefix != 0xFF)
                    continue;

                int marker;
                do
                {
                    marker = stream.ReadByte();
                    if (marker < 0)
                        return false;
                } while (marker == 0xFF);

                if (marker == 0xD9 || marker == 0xDA)
                    break;

                var markerHasNoLength = marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7);
                if (markerHasNoLength)
                    continue;

                var lengthBytes = new byte[2];
                if (!TryReadExactly(stream, lengthBytes, 2))
                    return false;
                var segmentLength = (lengthBytes[0] << 8) | lengthBytes[1];
                if (segmentLength < 2)
                    return false;

                var isSofMarker =
                    (marker >= 0xC0 && marker <= 0xC3) ||
                    (marker >= 0xC5 && marker <= 0xC7) ||
                    (marker >= 0xC9 && marker <= 0xCB) ||
                    (marker >= 0xCD && marker <= 0xCF);

                if (isSofMarker)
                {
                    var sofPayload = new byte[5];
                    if (!TryReadExactly(stream, sofPayload, sofPayload.Length))
                        return false;

                    height = (sofPayload[1] << 8) | sofPayload[2];
                    width = (sofPayload[3] << 8) | sofPayload[4];
                    return width > 0 && height > 0;
                }

                var skipLength = segmentLength - 2;
                if (stream.Position + skipLength > stream.Length)
                    return false;
                stream.Seek(skipLength, SeekOrigin.Current);
            }

            return false;
        }

        private static bool TryReadExactly(Stream stream, byte[] buffer, int count)
        {
            var offset = 0;
            while (offset < count)
            {
                var read = stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                    return false;
                offset += read;
            }

            return true;
        }

        private int? GetCurrentUserId()
        {
            // Try legacy numeric claim first, then common JWT identifiers.
            var userIdClaim = User.FindFirst("UserId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
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
                AltText = "", // Required field, cannot be null
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