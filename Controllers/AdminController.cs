using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PathForge.Data;
using PathForge.Helpers;

namespace PathForge.Controllers
{
    public class AdminController : Controller
    {
        private readonly PathForgeDbContext _db;
        public AdminController(PathForgeDbContext db) { _db = db; }

        // ── GET /Admin — Dashboard overview ─────────────────────────
        public async Task<IActionResult> Index()
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Admin"))
                return RedirectToAction("Login", "Auth");

            ViewBag.TotalUsers      = await _db.Users.CountAsync();
            ViewBag.ActiveCourses   = await _db.Courses.CountAsync(c => c.Status == "Live");
            ViewBag.TotalEnrolments = await _db.CourseEnrolments.CountAsync();
            ViewBag.FlaggedPosts    = await _db.DiscussionPosts.CountAsync(p => p.IsFlagged);

            var recentUsers = await _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .ToListAsync();

            return View(recentUsers);
        }

        // ── POST /Admin/SuspendUser/5 ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendUser(int id)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Admin"))
                return RedirectToAction("Login", "Auth");

            var user = await _db.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _db.SaveChangesAsync();
                TempData["Success"] = user.IsActive ? "User reactivated." : "User suspended.";
            }
            return RedirectToAction("Users");
        }

        // ── GET /Admin/Users — All users table ───────────────────────
        public async Task<IActionResult> Users()
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Admin"))
                return RedirectToAction("Login", "Auth");

            return View(await _db.Users.OrderBy(u => u.Role).ToListAsync());
        }

        // ── GET /Admin/Courses — All courses ─────────────────────────
        public async Task<IActionResult> Courses()
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Admin"))
                return RedirectToAction("Login", "Auth");

            return View(await _db.Courses
                .Include(c => c.Educator)
                .Include(c => c.Enrolments)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync());
        }
    }
}
