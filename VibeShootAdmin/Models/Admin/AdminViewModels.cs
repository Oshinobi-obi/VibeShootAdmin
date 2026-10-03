using System;
using System.Collections.Generic;
using VibeShootAdmin.Models.Entities;

namespace VibeShootAdmin.Models.Admin
{
    public class MonthlyRevenue
    {
        public string Label { get; set; } = "";
        public decimal Amount { get; set; }
        public int Bookings { get; set; }
    }

    public class DashboardViewModel
    {
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedUpcoming { get; set; }
        public int PaymentsForVerification { get; set; }
        public decimal AmountForVerification { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueLastMonth { get; set; }
        public decimal OutstandingBalance { get; set; }
        public List<MonthlyRevenue> Monthly { get; set; } = new List<MonthlyRevenue>();
        public Dictionary<string, int> StatusCounts { get; set; } = new Dictionary<string, int>();
        public List<Booking> Upcoming { get; set; } = new List<Booking>();
        public List<Payment> RecentPayments { get; set; } = new List<Payment>();
    }

    public class PagedFilter
    {
        public int? PhotographerId { get; set; }
        public string? Status { get; set; }
        public string? Q { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Method { get; set; }
        public int Page { get; set; } = 1;
    }

    public class BookingsViewModel
    {
        public PagedFilter Filter { get; set; } = new PagedFilter();
        public List<Booking> Items { get; set; } = new List<Booking>();
        public Dictionary<string, int> StatusCounts { get; set; } = new Dictionary<string, int>();
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class TransactionsViewModel
    {
        public PagedFilter Filter { get; set; } = new PagedFilter();
        public List<Payment> Items { get; set; } = new List<Payment>();
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public decimal VerifiedTotal { get; set; }
        public decimal ForVerificationTotal { get; set; }
        public decimal RejectedTotal { get; set; }
        public int ForVerificationCount { get; set; }
    }

    public class ReportViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string PhotographerName { get; set; } = "All photographers";
        public List<Payment> Payments { get; set; } = new List<Payment>();
        public string PreparedBy { get; set; } = "";
    }

    public class GalleryAdminViewModel
    {
        public int? PhotographerId { get; set; }
        public string? Category { get; set; }
        public List<GalleryImage> Images { get; set; } = new List<GalleryImage>();
        public Dictionary<string, int> CategoryCounts { get; set; } = new Dictionary<string, int>();
    }

    public class StudioViewModel
    {
        public Photographer Photographer { get; set; } = new Photographer();
        public List<ServicePackage> Packages { get; set; } = new List<ServicePackage>();
        public List<Entities.Admin> Accounts { get; set; } = new List<Entities.Admin>();
    }

    public class ProfileForm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Tagline { get; set; }
        public string? Bio { get; set; }
        public string? GCashAccountName { get; set; }
        public string? GCashNumber { get; set; }
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? TikTokUrl { get; set; }
        public string? XUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public Microsoft.AspNetCore.Http.IFormFile? Logo { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile? GCashQr { get; set; }
    }

    public class PackageForm
    {
        public int Id { get; set; }
        public int PhotographerId { get; set; }
        public string Category { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int DurationHours { get; set; } = 2;
        public string? Inclusions { get; set; }
    }
}

namespace VibeShootAdmin.Models.Admin
{
    public record PhotographerOption(int Id, string Name, string LogoPath);
}
