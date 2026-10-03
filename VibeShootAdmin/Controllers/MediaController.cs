using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using VibeShootAdmin.Data;

namespace VibeShootAdmin.Controllers
{
    /// <summary>Serves images stored in the database.</summary>
    public class MediaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MediaController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("media/{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var etag = $"\"{id:N}\"";
            if (Request.Headers.IfNoneMatch.ToString() == etag) return StatusCode(304);

            var media = await _context.MediaFiles
                .Where(m => m.Id == id)
                .Select(m => new { m.Data, m.ContentType, m.Kind })
                .FirstOrDefaultAsync();
            if (media == null) return NotFound();

            // A stored image never changes (a replacement gets a new id), so browsers can keep it.
            // Payment screenshots are personal, so only the viewer's own browser may cache them.
            var scope = media.Kind == VibeShootAdmin.Models.Entities.MediaKind.Receipt ? "private" : "public";
            Response.Headers.CacheControl = $"{scope}, max-age=31536000, immutable";
            Response.Headers.ETag = etag;
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(media.Data, media.ContentType);
        }
    }
}
