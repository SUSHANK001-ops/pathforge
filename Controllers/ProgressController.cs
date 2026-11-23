using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PathForge.Data;
using PathForge.Helpers;
using PathForge.Models.Entities;

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
            await IssueCertificateIfComplete(userId, module?.CourseId ?? 0);
            return RedirectToAction("Index");
        }

        // Issues a certificate the first time a student completes every module
        // in a course. Safe to call repeatedly — it no-ops if already issued
        // or the course isn't fully complete.
        private async Task IssueCertificateIfComplete(int userId, int courseId)
        {
            if (courseId == 0) return;

            var moduleIds = await _db.Modules
                .Where(m => m.CourseId == courseId)
                .Select(m => m.Id)
                .ToListAsync();
            if (moduleIds.Count == 0) return;

            int completed = await _db.StudentProgress
                .CountAsync(p => p.StudentId == userId &&
                                 moduleIds.Contains(p.ModuleId) &&
                                 p.Status == "Completed");
            if (completed < moduleIds.Count) return;

            bool exists = await _db.Certificates
                .AnyAsync(c => c.StudentId == userId && c.CourseId == courseId);
            if (exists) return;

            _db.Certificates.Add(new Certificate
            {
                StudentId  = userId,
                CourseId   = courseId,
                SerialCode = $"PF-{courseId:D3}-{userId:D3}-{DateTime.Now:yyyyMMdd}",
                IssuedAt   = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }
    }
}
