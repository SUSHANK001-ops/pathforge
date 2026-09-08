using Microsoft.AspNetCore.Mvc;
using PathForge.Data;

namespace PathForge.Controllers
{
    public class AssignmentController : Controller
    {
        private readonly PathForgeDbContext _context;

        public AssignmentController(PathForgeDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        public IActionResult Submit(int id)
        {
            return View();
        }
    }
}
