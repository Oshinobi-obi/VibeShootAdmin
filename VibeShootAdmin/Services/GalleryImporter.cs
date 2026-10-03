using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VibeShootAdmin.Data;
using VibeShootAdmin.Models.Entities;

namespace VibeShootAdmin.Services
{
    /// <summary>Registers photos copied straight into Uploads/Album/{MediaFolder}/{Category} that the database doesn't know about yet.</summary>
    public class GalleryImporter
    {
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly ApplicationDbContext _db;
        private readonly string _root;

        public GalleryImporter(ApplicationDbContext db, SiteOptions site)
        {
            _db = db;
            _root = site.MediaRootFullPath;
        }

        public async Task<int> ImportAsync()
        {
            var known = (await _db.GalleryImages.Select(g => g.FilePath).ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var photographers = await _db.Photographers.ToListAsync();
            int added = 0;

            foreach (var p in photographers.Where(p => !string.IsNullOrEmpty(p.MediaFolder)))
            {
                foreach (var category in GalleryImage.Categories)
                {
                    var dir = Path.Combine(_root, "Uploads", "Album", p.MediaFolder, category);
                    if (!Directory.Exists(dir)) continue;

                    var files = Directory.GetFiles(dir)
                        .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                        .OrderBy(f => NaturalKey(Path.GetFileName(f)))
                        .ToList();

                    int order = await _db.GalleryImages.Where(g => g.PhotographerId == p.Id && g.Category == category)
                        .MaxAsync(g => (int?)g.SortOrder) ?? 0;
                    foreach (var file in files)
                    {
                        var url = $"/Uploads/Album/{p.MediaFolder}/{category}/{Path.GetFileName(file)}";
                        if (known.Contains(url)) continue;

                        _db.GalleryImages.Add(new GalleryImage
                        {
                            PhotographerId = p.Id,
                            Category = category,
                            FilePath = url,
                            FileSizeBytes = new FileInfo(file).Length,
                            SortOrder = ++order,
                        });
                        known.Add(url);
                        added++;
                    }
                }
            }

            await _db.SaveChangesAsync();
            return added;
        }

        /// <summary>Sorts "GS2" before "GS10".</summary>
        private static string NaturalKey(string name) =>
            Regex.Replace(name, @"\d+", m => m.Value.PadLeft(8, '0'));
    }
}
