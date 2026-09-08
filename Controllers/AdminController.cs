using Microsoft.AspNetCore.Mvc;
using PathForge.Data;

namespace PathForge.Controllers
{
    public class AdminController : Controller
    {
        private readonly PathForgeDbContext _context;

        public AdminController(PathForgeDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Users()
        {
            return View();
        }

        public IActionResult Courses()
        {
            return View();
        }
    }
}
