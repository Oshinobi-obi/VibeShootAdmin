using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;
using VibeShootAdmin.Data;
using VibeShootAdmin.Models.Entities;

namespace VibeShootAdmin.Services
{
    /// <summary>Stores uploaded images in the MediaFiles table and hands back their /media/{id} URL.</summary>
    public class MediaStore
    {
        /// <summary>Browsers shrink photos before upload; this is the server-side ceiling.</summary>
        public const long MaxBytes = 5 * 1024 * 1024;

        private readonly ApplicationDbContext _db;

        public MediaStore(ApplicationDbContext db)
        {
            _db = db;
        }

        public static string? Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0) return "Please choose an image file.";
            if (file.Length > MaxBytes) return "Image is too large (max 5 MB).";
            return null;
        }

        /// <summary>
        /// Adds the upload to the context (saved with the caller's next SaveChanges) and returns its URL,
        /// or null if the file isn't a JPG, PNG or WEBP image.
        /// </summary>
        public async Task<string?> AddAsync(IFormFile file, string kind)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            return Add(ms.ToArray(), Path.GetFileName(file.FileName), kind);
        }

        public string? Add(byte[] data, string fileName, string kind, string? sourcePath = null)
        {
            var contentType = DetectImageType(data);
            if (contentType == null) return null;

            var media = new MediaFile
            {
                FileName = fileName.Length > 255 ? fileName[..255] : fileName,
                ContentType = contentType,
                Data = data,
                SizeBytes = data.LongLength,
                Kind = kind,
                SourcePath = sourcePath,
            };
            _db.MediaFiles.Add(media);
            return media.Url;
        }

        /// <summary>Removes a stored image by its /media/{id} URL (ignores anything else).</summary>
        public async Task DeleteAsync(string? url)
        {
            if (!TryParseId(url, out var id)) return;
            await _db.MediaFiles.Where(m => m.Id == id).ExecuteDeleteAsync();
        }

        public static bool TryParseId(string? url, out Guid id)
        {
            id = Guid.Empty;
            return url != null && url.StartsWith("/media/", StringComparison.OrdinalIgnoreCase)
                   && Guid.TryParse(url["/media/".Length..], out id);
        }

        /// <summary>Checks the file's first bytes rather than trusting its name or browser-reported type.</summary>
        public static string? DetectImageType(byte[] d)
        {
            if (d.Length > 3 && d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF) return "image/jpeg";
            if (d.Length > 8 && d[0] == 0x89 && d[1] == 0x50 && d[2] == 0x4E && d[3] == 0x47) return "image/png";
            if (d.Length > 12 && d[0] == 'R' && d[1] == 'I' && d[2] == 'F' && d[3] == 'F' && d[8] == 'W' && d[9] == 'E' && d[10] == 'B' && d[11] == 'P') return "image/webp";
            return null;
        }
    }
}
