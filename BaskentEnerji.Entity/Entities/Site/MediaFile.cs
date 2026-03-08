using System;

namespace BaskentEnerji.Entity.Entities.Site
{
    public class MediaFile
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string OriginalName { get; set; }
        public string FileUrl { get; set; }
        public string FileType { get; set; } // image, document, video
        public string MimeType { get; set; }
        public long FileSize { get; set; } // in bytes
        public int? Width { get; set; } // for images
        public int? Height { get; set; } // for images
        public string AltText { get; set; }
        public string Description { get; set; }
        public string FilePath { get; set; }
        public string Title { get; set; }
        public string FolderPath { get; set; }
        public string Tags { get; set; } // comma separated
        public int? UploadedBy { get; set; } // User ID
        public DateTime UploadedAt { get; set; }
        public DateTime? LastModified { get; set; }
        public bool IsActive { get; set; }
    }
}