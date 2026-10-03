using System;
using System.ComponentModel.DataAnnotations;

namespace VibeShootAdmin.Models.Entities
{
    public class Admin
    {
        public int Id { get; set; }

        [MaxLength(64)]
        public string Username { get; set; } = "";

        public string PasswordHash { get; set; } = "";

        /// <summary>"SuperAdmin" sees every photographer; "Photographer" only sees their own.</summary>
        [MaxLength(32)]
        public string Role { get; set; } = AdminRoles.Photographer;

        public int? PhotographerId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Photographer? Photographer { get; set; }
    }

    public static class AdminRoles
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Photographer = "Photographer";
    }
}
