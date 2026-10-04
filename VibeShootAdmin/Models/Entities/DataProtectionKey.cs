using System.ComponentModel.DataAnnotations;

namespace VibeShootAdmin.Models.Entities
{
    /// <summary>
    /// ASP.NET Core's data-protection keys (they sign the admin sign-in cookie and protect forms).
    /// Stored in the database so they survive the host restarting or recycling the app;
    /// otherwise everyone would be signed out and open forms would fail after each restart.
    /// </summary>
    public class DataProtectionKey
    {
        public int Id { get; set; }

        /// <summary>Which app the key belongs to ("VibeShoot" or "VibeShootAdmin"); each app keeps its own keys.</summary>
        [MaxLength(64)]
        public string App { get; set; } = "";

        [MaxLength(128)]
        public string? FriendlyName { get; set; }

        public string Xml { get; set; } = "";
    }
}
