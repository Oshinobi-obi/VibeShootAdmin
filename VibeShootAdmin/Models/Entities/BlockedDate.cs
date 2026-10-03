using System;
using System.ComponentModel.DataAnnotations;

namespace VibeShootAdmin.Models.Entities
{
    public class BlockedDate
    {
        [Key]
        public int Id { get; set; }
        public int PhotographerId { get; set; }
        public DateTime Date { get; set; }

        [MaxLength(256)]
        public string? Reason { get; set; }

        public Photographer? Photographer { get; set; }
    }
}
