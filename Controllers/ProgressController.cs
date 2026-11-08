using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PathForge.Data;
using PathForge.Helpers;

namespace PathForge.Controllers
{
    public class ProgressController : Controller
    {
        private readonly PathForgeDbContext _db;
        public ProgressController(PathForgeDbContext db) { _db = db; }

        // ── GET /Progress — Student's enrolled courses and progress ──
        public async Task<IActionResult> Index()
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Student"))
                return RedirectToAction("Login", "Auth");

            int userId = SessionHelper.GetUserId(HttpContext);

            var enrolments = await _db.CourseEnrolments
                .Include(e => e.Course)
                    .ThenInclude(c => c.Modules.OrderBy(m => m.DisplayOrder))
                .Where(e => e.StudentId == userId)
                .ToListAsync();

            // Load progress records for all enrolled courses
            var progressRecords = await _db.StudentProgress
                .Where(p => p.StudentId == userId)
                .ToListAsync();

            ViewBag.ProgressRecords = progressRecords;
            return View(enrolments);
        }

        // ── POST /Progress/MarkComplete — Mark a module done ────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkComplete(int moduleId)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Student"))
                return RedirectToAction("Login", "Auth");

            int userId = SessionHelper.GetUserId(HttpContext);

            var progress = await _db.StudentProgress
                .FirstOrDefaultAsync(p => p.StudentId == userId && p.ModuleId == moduleId);

            if (progress == null) return NotFound();
            if (progress.Status == "Completed") return RedirectToAction("Index");

            progress.Status      = "Completed";
            progress.CompletedAt = DateTime.Now;
            progress.UpdatedAt   = DateTime.Now;

            // Unlock the next module in the same course
            var module = await _db.Modules
                .Include(m => m.Course)
                    .ThenInclude(c => c.Modules.OrderBy(mm => mm.DisplayOrder))
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module != null)
            {
                var nextModule = module.Course.Modules
                    .FirstOrDefault(m => m.DisplayOrder == module.DisplayOrder + 1);

                if (nextModule != null)
                {
                    var nextProgress = await _db.StudentProgress
                        .FirstOrDefaultAsync(p => p.StudentId == userId &&
                                                   p.ModuleId == nextModule.Id);
                    if (nextProgress != null && nextProgress.Status == "Locked")
                    {
                        nextProgress.Status    = "InProgress";
                        nextProgress.StartedAt = DateTime.Now;
                        nextProgress.UpdatedAt = DateTime.Now;
                    }
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
