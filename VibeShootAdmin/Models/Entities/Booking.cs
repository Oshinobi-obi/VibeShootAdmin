using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace VibeShootAdmin.Models.Entities
{
    public class Booking
    {
        [Key]
        [MaxLength(32)]
        public string TransactionId { get; set; } = "";

        /// <summary>Random secret that lets a client open their receipt without logging in.</summary>
        [MaxLength(64)]
        public string AccessToken { get; set; } = "";

        public int PhotographerId { get; set; }
        public int? PackageId { get; set; }

        [MaxLength(128)] public string ClientName { get; set; } = "";
        [MaxLength(32)] public string ContactNumber { get; set; } = "";
        [MaxLength(128)] public string? Email { get; set; }
        [MaxLength(512)] public string? SocialLink { get; set; }

        public DateTime TargetDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        [MaxLength(256)] public string Venue { get; set; } = "";
        [MaxLength(64)] public string Category { get; set; } = "";
        [MaxLength(128)] public string PackageName { get; set; } = "";

        /// <summary>What the client pays for the package (after any discount).</summary>
        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalPrice { get; set; }

        /// <summary>Regular package rate at the time of booking (before discount).</summary>
        [Column(TypeName = "decimal(12,2)")]
        public decimal OriginalPrice { get; set; }

        /// <summary>Pesos taken off by the photographer's discount (0 = none).</summary>
        [Column(TypeName = "decimal(12,2)")]
        public decimal DiscountAmount { get; set; }

        [MaxLength(64)]
        public string? DiscountLabel { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal DownPaymentRequired { get; set; }

        public string? Notes { get; set; }
        public string? AdminRemarks { get; set; }

        [MaxLength(32)]
        public string Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Photographer? Photographer { get; set; }
        public ServicePackage? Package { get; set; }
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        [NotMapped]
        public decimal AmountPaid => Payments.Where(p => p.Status == PaymentStatus.Verified).Sum(p => p.Amount);

        [NotMapped]
        public decimal AmountForVerification => Payments.Where(p => p.Status == PaymentStatus.ForVerification).Sum(p => p.Amount);

        [NotMapped]
        public decimal Balance => Math.Max(0, TotalPrice - AmountPaid);

        [NotMapped]
        public string PaymentState =>
            TotalPrice <= 0 ? "Quotation"
            : AmountPaid >= TotalPrice ? "Fully Paid"
            : AmountPaid > 0 ? "Partially Paid"
            : AmountForVerification > 0 ? "For Verification"
            : "Unpaid";

        /// <summary>Changes whenever the booking or any of its payments changes (drives the client's live refresh).</summary>
        [NotMapped]
        public string LiveStamp =>
            $"{Status}|{UpdatedAt?.Ticks}|{string.Join(",", Payments.OrderBy(p => p.Id).Select(p => p.Id + ":" + p.Status))}";

        [NotMapped]
        public string TimeRange => $"{FormatTime(StartTime)} – {FormatTime(EndTime)}";

        public static string FormatTime(TimeSpan t) => DateTime.Today.Add(t).ToString("h:mm tt");

    }

    public static class BookingStatus
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Completed = "Completed";
        public const string Declined = "Declined";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Pending, Confirmed, Completed, Declined, Cancelled };
    }
}
