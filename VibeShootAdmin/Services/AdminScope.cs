using System.Linq;
using System.Security.Claims;
using VibeShootAdmin.Models.Entities;

namespace VibeShootAdmin.Services
{
    /// <summary>Restricts what a signed-in admin can see: a SuperAdmin sees everything, a Photographer only their own data.</summary>
    public static class AdminScope
    {
        public static bool IsSuperAdmin(this ClaimsPrincipal user) =>
            user.FindFirst("Role")?.Value == AdminRoles.SuperAdmin;

        /// <summary>The photographer this admin is locked to, or null for a SuperAdmin.</summary>
        public static int? ScopedPhotographerId(this ClaimsPrincipal user)
        {
            if (user.IsSuperAdmin()) return null;
            return int.TryParse(user.FindFirst("PhotographerId")?.Value, out var id) && id > 0 ? id : -1;
        }

        /// <summary>Applies the admin's scope, plus an optional photographer filter chosen in the UI.</summary>
        public static int? EffectivePhotographerId(this ClaimsPrincipal user, int? requested) =>
            user.ScopedPhotographerId() ?? (requested > 0 ? requested : null);

        public static bool CanAccess(this ClaimsPrincipal user, int photographerId)
        {
            var scoped = user.ScopedPhotographerId();
            return scoped == null || scoped == photographerId;
        }

        public static IQueryable<Booking> ForPhotographer(this IQueryable<Booking> q, int? photographerId) =>
            photographerId == null ? q : q.Where(b => b.PhotographerId == photographerId);

        public static IQueryable<Payment> ForPhotographer(this IQueryable<Payment> q, int? photographerId) =>
            photographerId == null ? q : q.Where(p => p.Booking!.PhotographerId == photographerId);
    }
}
