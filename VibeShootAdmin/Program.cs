using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VibeShootAdmin.Data;
using VibeShootAdmin.Models.Entities;
using VibeShootAdmin.Services;

var builder = WebApplication.CreateBuilder(args);

// Where the public VibeShoot site lives (used for links back to it). Both apps share the database,
// and every image is stored there, so the admin needs no access to the public site's files.
var site = builder.Configuration.GetSection("Site").Get<SiteOptions>() ?? new SiteOptions();
site.Resolve();
builder.Services.AddSingleton(site);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<MediaStore>();
builder.Services.AddVibeShootRateLimits();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddAuthentication("VibeShootAdminCookie")
    .AddCookie("VibeShootAdminCookie", options =>
    {
        options.Cookie.Name = "VibeShootAdmin.Auth";
        options.Cookie.HttpOnly = true;                       // page scripts can't read it
        options.Cookie.SameSite = SameSiteMode.Lax;           // not sent with form posts from other sites
        options.SlidingExpiration = true;
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

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

// HTTPS is off until the sites have SSL certificates. Set "Https:Enabled": true in appsettings to turn it back on.
var httpsEnabled = app.Configuration.GetValue<bool>("Https:Enabled");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    if (httpsEnabled) app.UseHsts();
}

if (httpsEnabled) app.UseHttpsRedirection();
app.UseSecurityHeaders();

// Old-style /Uploads/... image paths (from before images moved into the database) live on the public site.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Uploads"))
    {
        context.Response.Redirect(site.PublicUrl(context.Request.Path.Value!));
        return;
    }
    await next();
});

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
