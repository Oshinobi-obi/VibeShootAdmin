using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace VibeShootAdmin.Models.Entities
{
    /// <summary>
    /// A client's review of a photographer. Only possible from a real, finished booking
    /// (one review per booking), so every review is from a verified client.
    /// </summary>
    public class Review
    {
        public int Id { get; set; }
        public int PhotographerId { get; set; }

        /// <summary>The booking this review is for (unique: one review per booking).</summary>
        [MaxLength(32)]
        public string BookingTransactionId { get; set; } = "";

        /// <summary>1 to 5 stars.</summary>
        public int Rating { get; set; }

        /// <summary>Comma-separated values from <see cref="AllowedTags"/>.</summary>
        [MaxLength(512)]
        public string Tags { get; set; } = "";

        [MaxLength(600)]
        public string? Comment { get; set; }

        /// <summary>Shown publicly, e.g. "Maria S." (first name + last initial only).</summary>
        [MaxLength(64)]
        public string DisplayName { get; set; } = "";

        [MaxLength(64)]
        public string Category { get; set; } = "";

        /// <summary>Hidden by the Super Admin (e.g. abusive language). Hidden reviews aren't shown or counted.</summary>
        public bool IsHidden { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Photographer? Photographer { get; set; }
        public Booking? Booking { get; set; }

        [NotMapped]
        public IEnumerable<string> TagList => Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        public const int MaxComment = 600;

        /// <summary>The only tags a client can pick ("What did you like about this photographer?").</summary>
        public static readonly string[] AllowedTags =
        {
            "Professional", "Friendly", "On time", "Creative", "Great communication",
            "Made us comfortable", "Beautiful edits", "Fast delivery", "Worth the price",
        };

        /// <summary>"Maria Santos" → "Maria S." so a client's full name is never published.</summary>
        public static string ToDisplayName(string fullName)
        {
            var parts = (fullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "Client";
            var first = parts[0].Length > 30 ? parts[0][..30] : parts[0];
            return parts.Length > 1 ? $"{first} {char.ToUpperInvariant(parts[^1][0])}." : first;
        }

        /// <summary>A booking can be reviewed once the session is done.</summary>
        public static bool CanReview(Booking b, DateTime today) =>
            b.Status == BookingStatus.Completed
            || (b.Status == BookingStatus.Confirmed && b.TargetDate.Date < today.Date);
    }
}
