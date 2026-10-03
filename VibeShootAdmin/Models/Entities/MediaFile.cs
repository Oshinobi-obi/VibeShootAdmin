using System;
using System.ComponentModel.DataAnnotations;

namespace VibeShootAdmin.Models.Entities
{
    /// <summary>
    /// An image stored in the database (gallery photo, logo, GCash QR or payment screenshot).
    /// Served at /media/{Id} by both the public site and the admin console.
    /// </summary>
    public class MediaFile
    {
        /// <summary>Random id, so URLs of private images (payment screenshots) can't be guessed.</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(255)]
        public string FileName { get; set; } = "";

        [MaxLength(100)]
        public string ContentType { get; set; } = "image/jpeg";

        public byte[] Data { get; set; } = Array.Empty<byte>();

        public long SizeBytes { get; set; }

        /// <summary>Gallery, Logo, QRCode or Receipt.</summary>
        [MaxLength(32)]
        public string Kind { get; set; } = MediaKind.Gallery;

        /// <summary>Original wwwroot path for images imported from the project's Uploads folder (prevents re-importing).</summary>
        [MaxLength(512)]
        public string? SourcePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Url => MediaUrl(Id);

        public static string MediaUrl(Guid id) => $"/media/{id:N}";
    }

    public static class MediaKind
    {
        public const string Gallery = "Gallery";
        public const string Logo = "Logo";
        public const string QrCode = "QRCode";
        public const string Receipt = "Receipt";
    }
}
