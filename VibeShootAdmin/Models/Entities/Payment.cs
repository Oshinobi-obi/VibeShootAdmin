using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VibeShootAdmin.Models.Entities
{
    /// <summary>One money movement against a booking (GCash down payment, balance, etc.).</summary>
    public class Payment
    {
        public int Id { get; set; }

        [MaxLength(32)]
        public string BookingTransactionId { get; set; } = "";

        /// <summary>Human-readable receipt number, e.g. OR-2026-000123.</summary>
        [MaxLength(32)]
        public string ReceiptNumber { get; set; } = "";

        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [MaxLength(32)] public string Method { get; set; } = PaymentMethod.GCash;
        [MaxLength(32)] public string Type { get; set; } = PaymentType.DownPayment;

        /// <summary>GCash reference number typed by the client (13 digits on GCash receipts).</summary>
        [MaxLength(64)] public string? ReferenceNumber { get; set; }

        [MaxLength(512)] public string? ProofImagePath { get; set; }

        [MaxLength(32)] public string Status { get; set; } = PaymentStatus.ForVerification;

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }
        [MaxLength(64)] public string? VerifiedBy { get; set; }

        public Booking? Booking { get; set; }
    }

    public static class PaymentStatus
    {
        public const string ForVerification = "For Verification";
        public const string Verified = "Verified";
        public const string Rejected = "Rejected";

        public static readonly string[] All = { ForVerification, Verified, Rejected };
    }

    public static class PaymentMethod
    {
        public const string GCash = "GCash";
        public const string Cash = "Cash";
    }

    public static class PaymentType
    {
        public const string DownPayment = "Down Payment";
        public const string Balance = "Balance";
    }
}
