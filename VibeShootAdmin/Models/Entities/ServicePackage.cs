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

        /// <summary>0 means custom / quotation — no down payment is collected online.</summary>
        [Column(TypeName = "decimal(12,2)")]
        public decimal Price { get; set; }

        public int DurationHours { get; set; } = 2;

        /// <summary>Inclusions, one per line.</summary>
        public string Inclusions { get; set; } = "";

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public Photographer? Photographer { get; set; }

        [NotMapped]
        public IEnumerable<string> InclusionList =>
            Inclusions.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        public static readonly string[] Categories = { "Birthday", "Baptism", "Photoshoot", "Wedding" };
    }
}
