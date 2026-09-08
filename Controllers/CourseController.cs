using Microsoft.AspNetCore.Mvc;
using PathForge.Data;
using PathForge.Models.ViewModels;

namespace PathForge.Controllers
{
    public class CourseController : Controller
    {
        private readonly PathForgeDbContext _context;

        public CourseController(PathForgeDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        public IActionResult Edit(int id)
        {
            return View();
        }

        public IActionResult Manage()
        {
            return View("Index");
        }
    }
}
