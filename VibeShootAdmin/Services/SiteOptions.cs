namespace VibeShootAdmin.Services
{
    /// <summary>Where the public VibeShoot site lives. Both apps share the same database.</summary>
    public class SiteOptions
    {
        /// <summary>Base URL of the public site, e.g. https://vibeshoot.runasp.net.</summary>
        public string PublicSiteUrl { get; set; } = "http://localhost:5041";

        public void Resolve()
        {
            PublicSiteUrl = PublicSiteUrl.TrimEnd('/');
        }

        public string PublicUrl(string path) => PublicSiteUrl + "/" + path.TrimStart('/');
    }
}
