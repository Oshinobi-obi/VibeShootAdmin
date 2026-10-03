using System;
using System.ComponentModel.DataAnnotations;

namespace VibeShootAdmin.Models.Entities
{
    /// <summary>A portfolio photo. The file lives under wwwroot/Uploads; the database holds its record.</summary>
    public class GalleryImage
    {
        public int Id { get; set; }
        public int PhotographerId { get; set; }

        [MaxLength(64)]
        public string Category { get; set; } = "";

        [MaxLength(512)]
        public string FilePath { get; set; } = "";

        [MaxLength(256)]
        public string? Caption { get; set; }

        public long FileSizeBytes { get; set; }
        public int SortOrder { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public Photographer? Photographer { get; set; }

        public static readonly string[] Categories = { "Birthday", "Baptism", "Wedding", "Highlights" };
    }
}
