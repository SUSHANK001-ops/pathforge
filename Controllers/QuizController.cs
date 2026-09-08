using Microsoft.AspNetCore.Mvc;
using PathForge.Data;

namespace PathForge.Controllers
{
    public class QuizController : Controller
    {
        private readonly PathForgeDbContext _context;

        public QuizController(PathForgeDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Take(int id)
        {
            return View();
        }

        public IActionResult Result(int id)
        {
            return View();
        }
    }
}
