using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using VibeShootAdmin.Data;
using VibeShootAdmin.Models.Entities;

namespace VibeShootAdmin.Services
{
    /// <summary>Keeps ASP.NET Core data-protection keys in the database (DataProtectionKeys table).</summary>
    public class DbKeyRepository : IXmlRepository
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly string _app;

        public DbKeyRepository(IServiceScopeFactory scopes, string app)
        {
            _scopes = scopes;
            _app = app;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return db.DataProtectionKeys
                .Where(k => k.App == _app)
                .Select(k => k.Xml)
                .ToList()
                .Select(XElement.Parse)
                .ToList()
                .AsReadOnly();
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.DataProtectionKeys.Add(new DataProtectionKey
            {
                App = _app,
                FriendlyName = friendlyName,
                Xml = element.ToString(SaveOptions.DisableFormatting),
            });
            db.SaveChanges();
        }
    }

    public static class DataProtectionSetup
    {
        /// <summary>Store keys in the database under <paramref name="appName"/> so sign-ins and forms survive restarts.</summary>
        public static IServiceCollection AddDatabaseDataProtection(this IServiceCollection services, string appName)
        {
            services.AddDataProtection().SetApplicationName(appName);
            services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(sp =>
                new ConfigureOptions<KeyManagementOptions>(o =>
                    o.XmlRepository = new DbKeyRepository(sp.GetRequiredService<IServiceScopeFactory>(), appName)));
            return services;
        }
    }
}
