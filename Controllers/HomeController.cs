using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PathForge.Data;
using PathForge.Models;

namespace PathForge.Controllers
{
    public class HomeController : Controller
    {
        private readonly PathForgeDbContext _db;

        public HomeController(PathForgeDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            // Pass session data to the view via ViewBag so the layout can use it
            ViewBag.IsLoggedIn = !string.IsNullOrEmpty(
                HttpContext.Session.GetString("UserId"));
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole") ?? "";
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
