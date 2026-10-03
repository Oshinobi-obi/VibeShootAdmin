using System;
using System.IO;

namespace VibeShootAdmin.Services
{
    /// <summary>
    /// Where the public VibeShoot site lives. The admin console shares its database and its
    /// wwwroot/Uploads folder (photos, logos, GCash QR codes, payment screenshots).
    /// </summary>
    public class SiteOptions
    {
        /// <summary>Base URL of the public site, e.g. http://localhost:5041.</summary>
        public string PublicSiteUrl { get; set; } = "http://localhost:5041";

        /// <summary>Path to the public site's wwwroot folder (absolute, or relative to this project).</summary>
        public string MediaRoot { get; set; } = "../../VibeShoot/VibeShoot/wwwroot";

        /// <summary>Absolute media root, resolved once at startup.</summary>
        public string MediaRootFullPath { get; private set; } = "";

        public void Resolve(string contentRoot)
        {
            MediaRootFullPath = Path.GetFullPath(Path.IsPathRooted(MediaRoot) ? MediaRoot : Path.Combine(contentRoot, MediaRoot));
            PublicSiteUrl = PublicSiteUrl.TrimEnd('/');
        }

        public string PublicUrl(string path) => PublicSiteUrl + "/" + path.TrimStart('/');
    }
}
