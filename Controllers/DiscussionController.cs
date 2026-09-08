using Microsoft.AspNetCore.Mvc;
using PathForge.Data;

namespace PathForge.Controllers
{
    public class DiscussionController : Controller
    {
        private readonly PathForgeDbContext _context;

        public DiscussionController(PathForgeDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Thread(int id)
        {
            return View();
        }
    }
}
