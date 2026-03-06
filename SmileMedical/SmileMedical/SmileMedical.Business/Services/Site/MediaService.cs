using SmileMedical.Business.Infrastructure.Site;
using SmileMedical.Data.Contexts;
using SmileMedical.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SmileMedical.Business.Services.Site
{
    public class MediaService : IMediaService
    {
        private readonly SmileMedicalDbContext _dbContext;

        public MediaService(SmileMedicalDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<MediaFile>> GetAll()
        {
            return await _dbContext.MediaFiles
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();
        }

        public async Task<MediaFile> GetById(int id)
        {
            return await _dbContext.MediaFiles
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<MediaFile> Add(MediaFile mediaFile)
        {
            mediaFile.UploadedAt = DateTime.UtcNow;
            _dbContext.MediaFiles.Add(mediaFile);
            await _dbContext.SaveChangesAsync();
            return mediaFile;
        }

        public async Task Update(MediaFile mediaFile)
        {
            _dbContext.MediaFiles.Update(mediaFile);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var mediaFile = await _dbContext.MediaFiles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (mediaFile != null)
            {
                _dbContext.MediaFiles.Remove(mediaFile);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<List<MediaFile>> GetByType(string fileType)
        {
            return await _dbContext.MediaFiles
                .Where(m => m.FileType == fileType)
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();
        }

        public async Task<List<MediaFile>> Search(string query)
        {
            return await _dbContext.MediaFiles
                .Where(m => m.FileName.Contains(query) || 
                           m.AltText.Contains(query) || 
                           m.Title.Contains(query))
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();
        }
    }
}