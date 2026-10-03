using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VibeShootAdmin.Models.Entities
{
    public class Photographer
    {
        public int Id { get; set; }

        [MaxLength(64)]
        public string Slug { get; set; } = "";

        [MaxLength(128)]
        public string Name { get; set; } = "";

        [MaxLength(256)]
        public string Tagline { get; set; } = "";

        public string Bio { get; set; } = "";

        /// <summary>Folder name under wwwroot/Uploads/{Album,Logos,QRCodes} for this photographer's files.</summary>
        [MaxLength(64)]
        public string MediaFolder { get; set; } = "";

        [MaxLength(512)]
        public string LogoPath { get; set; } = "";

        [MaxLength(512)]
        public string? GCashQrPath { get; set; }

        [MaxLength(128)]
        public string? GCashAccountName { get; set; }

        [MaxLength(32)]
        public string? GCashNumber { get; set; }

        [MaxLength(512)] public string? FacebookUrl { get; set; }
        [MaxLength(512)] public string? InstagramUrl { get; set; }
        [MaxLength(512)] public string? TikTokUrl { get; set; }
        [MaxLength(512)] public string? XUrl { get; set; }

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<GalleryImage> GalleryImages { get; set; } = new List<GalleryImage>();
        public ICollection<ServicePackage> Packages { get; set; } = new List<ServicePackage>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
