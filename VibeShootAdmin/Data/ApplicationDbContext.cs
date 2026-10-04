using Microsoft.EntityFrameworkCore;
using VibeShootAdmin.Models.Entities;

namespace VibeShootAdmin.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Photographer> Photographers { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ServicePackage> Packages { get; set; }
        public DbSet<GalleryImage> GalleryImages { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<BlockedDate> BlockedDates { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Photographer>(e =>
            {
                e.HasIndex(p => p.Slug).IsUnique();
            });

            modelBuilder.Entity<Admin>(e =>
            {
                e.HasIndex(a => a.Username).IsUnique();
                e.HasOne(a => a.Photographer).WithMany().HasForeignKey(a => a.PhotographerId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Booking>(e =>
            {
                e.HasIndex(b => new { b.PhotographerId, b.TargetDate });
                e.HasIndex(b => b.Status);
                e.HasOne(b => b.Photographer).WithMany(p => p.Bookings).HasForeignKey(b => b.PhotographerId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(b => b.Package).WithMany().HasForeignKey(b => b.PackageId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Payment>(e =>
            {
                e.HasIndex(p => p.ReceiptNumber).IsUnique();
                e.HasIndex(p => p.Status);
                e.HasIndex(p => p.ReferenceNumber);
                e.HasOne(p => p.Booking).WithMany(b => b.Payments).HasForeignKey(p => p.BookingTransactionId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ServicePackage>(e =>
            {
                e.HasOne(p => p.Photographer).WithMany(p => p.Packages).HasForeignKey(p => p.PhotographerId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<GalleryImage>(e =>
            {
                e.HasIndex(g => new { g.PhotographerId, g.Category });
                e.HasIndex(g => g.FilePath).IsUnique();
                e.HasOne(g => g.Photographer).WithMany(p => p.GalleryImages).HasForeignKey(g => g.PhotographerId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Review>(e =>
            {
                e.HasIndex(r => r.BookingTransactionId).IsUnique();          // one review per booking
                e.HasIndex(r => new { r.PhotographerId, r.IsHidden, r.CreatedAt });
                e.HasOne(r => r.Photographer).WithMany().HasForeignKey(r => r.PhotographerId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(r => r.Booking).WithMany().HasForeignKey(r => r.BookingTransactionId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DataProtectionKey>(e =>
            {
                e.HasIndex(k => k.App);
                e.Property(k => k.Xml).HasColumnType("longtext");
            });

            modelBuilder.Entity<MediaFile>(e =>
            {
                e.Property(m => m.Data).HasColumnType("longblob");
                e.HasIndex(m => m.SourcePath);
                e.Ignore(m => m.Url);
            });

            modelBuilder.Entity<BlockedDate>(e =>
            {
                e.HasIndex(b => new { b.PhotographerId, b.Date }).IsUnique();
                e.HasOne(b => b.Photographer).WithMany().HasForeignKey(b => b.PhotographerId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
