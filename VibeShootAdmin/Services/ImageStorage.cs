using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VibeShootAdmin.Services
{
    /// <summary>Saves uploaded images into the public site's wwwroot/Uploads and returns their URL path.</summary>
    public class ImageStorage
    {
        public const long MaxBytes = 10 * 1024 * 1024;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly string _root;

        public ImageStorage(SiteOptions site)
        {
            _root = site.MediaRootFullPath;
        }

        public static string? Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0) return "Please choose an image file.";
            if (file.Length > MaxBytes) return "Image is too large (max 10 MB).";

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext)) return "Only JPG, PNG or WEBP images are allowed.";
            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return "The uploaded file is not an image.";

            return null;
        }

        /// <param name="relativeFolder">Folder under the media root, e.g. "Uploads/Receipts".</param>
        /// <param name="fileNameStem">File name without extension; a short random suffix is added.</param>
        public async Task<string> SaveAsync(IFormFile file, string relativeFolder, string fileNameStem)
        {
            var folder = Path.Combine(_root, relativeFolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var safeStem = string.Concat(fileNameStem.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
            if (safeStem.Length > 40) safeStem = safeStem[..40];
            var fileName = $"{safeStem}_{Guid.NewGuid().ToString("N")[..8]}{ext}";

            await using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            return $"/{relativeFolder.Trim('/')}/{fileName}";
        }

        /// <summary>Deletes a file previously returned by <see cref="SaveAsync"/>; ignores paths outside Uploads.</summary>
        public void Delete(string? publicPath)
        {
            if (string.IsNullOrEmpty(publicPath) || !publicPath.StartsWith("/Uploads/", StringComparison.OrdinalIgnoreCase)) return;

            var full = Path.GetFullPath(Path.Combine(_root, publicPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
            var uploadsRoot = Path.GetFullPath(Path.Combine(_root, "Uploads"));
            if (full.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(full))
            {
                File.Delete(full);
            }
        }
    }
}
