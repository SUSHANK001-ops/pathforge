using Microsoft.AspNetCore.Mvc;
using PathForge.Data;

namespace PathForge.Controllers
{
    public class ProgressController : Controller
    {
        private readonly PathForgeDbContext _context;

        public ProgressController(PathForgeDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
