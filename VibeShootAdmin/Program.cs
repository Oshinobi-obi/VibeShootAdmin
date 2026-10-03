using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using VibeShootAdmin.Data;
using VibeShootAdmin.Models.Entities;
using VibeShootAdmin.Services;

var builder = WebApplication.CreateBuilder(args);

// Where the public VibeShoot site lives (shared database + shared Uploads folder).
var site = builder.Configuration.GetSection("Site").Get<SiteOptions>() ?? new SiteOptions();
site.Resolve(builder.Environment.ContentRootPath);
builder.Services.AddSingleton(site);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ImageStorage>();
builder.Services.AddScoped<GalleryImporter>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddAuthentication("VibeShootAdminCookie")
    .AddCookie("VibeShootAdminCookie", options =>
    {
        options.Cookie.Name = "VibeShootAdmin.Auth";
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

if (!Directory.Exists(Path.Combine(site.MediaRootFullPath, "Uploads")))
{
    app.Logger.LogWarning("Shared media folder not found at {Path}. Set Site:MediaRoot in appsettings.json to the VibeShoot site's wwwroot.", site.MediaRootFullPath);
}

// The database schema is owned by the public VibeShoot app (it runs the migrations).
// Here we only make sure there is an admin account to sign in with.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        if (!await db.Admins.AnyAsync())
        {
            var admin = new Admin { Username = "admin", Role = AdminRoles.SuperAdmin };
            admin.PasswordHash = new PasswordHasher<Admin>().HashPassword(admin, "Admin123!");
            db.Admins.Add(admin);
            await db.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Could not reach the VibeShoot database. Start the VibeShoot site once so it can create the database, then restart the admin console.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve photos, logos, QR codes and payment screenshots straight from the public site's folder.
if (Directory.Exists(Path.Combine(site.MediaRootFullPath, "Uploads")))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.Combine(site.MediaRootFullPath, "Uploads")),
        RequestPath = "/Uploads"
    });
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
