using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VibeShootAdmin.Models.Entities
{
    public class ServicePackage
    {
        public int Id { get; set; }
        public int PhotographerId { get; set; }

        [MaxLength(64)] public string Category { get; set; } = "";
        [MaxLength(128)] public string Name { get; set; } = "";

        /// <summary>Regular rate. 0 means custom / quotation — no down payment is collected online.</summary>
        [Column(TypeName = "decimal(12,2)")]
        public decimal Price { get; set; }

        public int DurationHours { get; set; } = 2;

        /// <summary>Inclusions, one per line.</summary>
        public string Inclusions { get; set; } = "";

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        // ---------------------------------------------------------------- Optional discount (set by the photographer)

        /// <summary>null = no discount; otherwise <see cref="DiscountKind.Percent"/> or <see cref="DiscountKind.Amount"/>.</summary>
        [MaxLength(16)]
        public string? DiscountType { get; set; }

        /// <summary>Percent off (1-90) or pesos off, depending on <see cref="DiscountType"/>.</summary>
        [Column(TypeName = "decimal(12,2)")]
        public decimal DiscountValue { get; set; }

        /// <summary>Shown to clients, e.g. "Holiday promo".</summary>
        [MaxLength(64)]
        public string? DiscountLabel { get; set; }

        /// <summary>Optional first and last day the discount applies (inclusive). Empty = no limit.</summary>
        public DateTime? DiscountStart { get; set; }
        public DateTime? DiscountEnd { get; set; }

        public Photographer? Photographer { get; set; }

        [NotMapped]
        public IEnumerable<string> InclusionList =>
            Inclusions.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        /// <summary>True when a discount is set up and today is inside its date window.</summary>
        public bool HasActiveDiscount(DateTime today) =>
            Price > 0 && DiscountType != null && DiscountValue > 0
            && (DiscountStart == null || today.Date >= DiscountStart.Value.Date)
            && (DiscountEnd == null || today.Date <= DiscountEnd.Value.Date);

        /// <summary>Pesos taken off the regular price today (0 when no discount applies).</summary>
        public decimal DiscountAmountOn(DateTime today)
        {
            if (!HasActiveDiscount(today)) return 0;
            var off = DiscountType == DiscountKind.Percent
                ? Math.Round(Price * Math.Min(DiscountValue, 90) / 100m, 0, MidpointRounding.AwayFromZero)
                : DiscountValue;
            return Math.Min(off, Price);
        }

        /// <summary>What the client pays for this package today.</summary>
        public decimal PriceOn(DateTime today) => Price - DiscountAmountOn(today);

        /// <summary>Short badge text, e.g. "20% OFF" or "₱500 OFF".</summary>
        public string DiscountBadge =>
            DiscountType == DiscountKind.Percent ? $"{DiscountValue:0.##}% OFF" : $"₱{DiscountValue:N0} OFF";

        public static readonly string[] Categories = { "Birthday", "Baptism", "Photoshoot", "Wedding" };
    }

    public static class DiscountKind
    {
        public const string Percent = "Percent";
        public const string Amount = "Amount";
    }
}
