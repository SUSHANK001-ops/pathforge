using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PathForge.Data;
using PathForge.Helpers;
using PathForge.Models.Entities;

namespace PathForge.Controllers
{
    /// <summary>
    /// Course-wide announcements posted by educators/admins and read by
    /// enrolled students.
    /// </summary>
    public class AnnouncementController : Controller
    {
        private readonly PathForgeDbContext _db;
        public AnnouncementController(PathForgeDbContext db) { _db = db; }

        // ── GET /Announcement?courseId=5 — list for a course ────────
        public async Task<IActionResult> Index(int courseId)
        {
            var course = await _db.Courses
                .Include(c => c.Educator)
                .FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null) return NotFound();

            int userId  = SessionHelper.GetUserId(HttpContext);
            string role = SessionHelper.GetUserRole(HttpContext);
            bool isManager = role == "Admin" || course.EducatorId == userId;
            bool enrolled  = userId > 0 &&
                await _db.CourseEnrolments.AnyAsync(e => e.CourseId == courseId && e.StudentId == userId);

            if (!isManager && !enrolled)
            {
                TempData["Error"] = "Enrol in the course to read its announcements.";
                return RedirectToAction("Details", "Course", new { id = courseId });
            }

            var items = await _db.Announcements
                .Include(a => a.Author)
                .Where(a => a.CourseId == courseId)
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            ViewBag.Course    = course;
            ViewBag.IsManager = isManager;
            return View(items);
        }

        // ── POST /Announcement/Create ───────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int courseId, string title, string body, bool isPinned)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var course = await _db.Courses.FindAsync(courseId);
            if (course == null) return NotFound();
            if (!CanManage(course)) return Forbid();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                TempData["Error"] = "Title and body are required.";
                return RedirectToAction("Index", new { courseId });
            }

            _db.Announcements.Add(new Announcement
            {
                CourseId = courseId,
                AuthorId = SessionHelper.GetUserId(HttpContext),
                Title    = title.Trim(),
                Body     = body.Trim(),
                IsPinned = isPinned
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Announcement posted.";
            return RedirectToAction("Index", new { courseId });
        }

        // ── POST /Announcement/Delete ───────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var ann = await _db.Announcements
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (ann == null) return NotFound();
            if (!CanManage(ann.Course)) return Forbid();

            int courseId = ann.CourseId;
            _db.Announcements.Remove(ann);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Announcement deleted.";
            return RedirectToAction("Index", new { courseId });
        }

        private bool CanManage(Course course)
        {
            int userId  = SessionHelper.GetUserId(HttpContext);
            string role = SessionHelper.GetUserRole(HttpContext);
            return role == "Admin" || course.EducatorId == userId;
        }
    }
}
