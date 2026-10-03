using VibeShootAdmin.Models.Entities;

namespace VibeShootAdmin.Models
{
    public class ReceiptViewModel
    {
        public Booking Booking { get; set; } = new Booking();

        /// <summary>When set, the page is the official receipt for this single payment; otherwise a booking statement.</summary>
        public Payment? Payment { get; set; }
    }
}
