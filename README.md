# VibeShootAdmin

Admin console for **[VibeShoot](https://github.com/Oshinobi-obi/VibeShoot)**: schedules, bookings, GCash payment verification,
receipts and gallery management. ASP.NET Core MVC (.NET 10) + MySQL (Pomelo EF Core).

It shares the **database** (`VibeShootStudio`) with the public VibeShoot site. The VibeShoot site owns the schema
and runs the migrations, so start it once first. All images, including photos, logos, GCash QR codes and client
payment screenshots, are stored in that database (`MediaFiles` table), so the two apps can run on different hosts.

## Getting started

1. Run the VibeShoot site once so the database exists.
2. Check `VibeShootAdmin/appsettings.json`:
   - `ConnectionStrings:DefaultConnection`: the same MySQL database as the VibeShoot site
   - `Site:PublicSiteUrl`: where the public site runs (used for "View public site" links)
3. Run it:
   ```
   dotnet run --project VibeShootAdmin --launch-profile http
   ```
4. Open http://localhost:5018 and sign in with **admin / Admin123!** (created on first start; change it under
   *Settings → Change your password*).

## Pages
- **Overview**: money collected this month, payments awaiting verification, pending requests, 6-month chart
- **Schedule**: month calendar of all sessions; block/unblock days off
- **Bookings**: filter/search; confirm, decline, complete or cancel; record cash payments; print statements
- **Transactions**: view GCash proofs, verify or reject payments, export CSV
- **Receipts**: reprint official receipts, print collection reports by date range
- **Gallery**: upload, delete and feature portfolio photos
- **Settings**: studio profile, GCash QR, packages and prices, admin accounts (Super Admin or per-photographer)

## Keeping the models in sync
`Models/Entities/*` and `Data/ApplicationDbContext.cs` are copies of the ones in the VibeShoot project.
When the schema changes, add the migration in VibeShoot and copy the same entity change here.
