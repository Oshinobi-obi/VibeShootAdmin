using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VibeShootAdmin.Models;

namespace VibeShootAdmin.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>The console has no landing page of its own; send people to the dashboard (or sign-in).</summary>
        public IActionResult Index() => RedirectToAction("Dashboard", "Admin");

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
