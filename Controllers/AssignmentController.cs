using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PathForge.Data;
using PathForge.Helpers;
using PathForge.Models.Entities;

namespace PathForge.Controllers
{
    public class AssignmentController : Controller
    {
        private readonly PathForgeDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AssignmentController(PathForgeDbContext db, IWebHostEnvironment env)
        {
            _db  = db;
            _env = env;
        }

        // ── GET /Assignment/Index/courseId — List assignments ────────
        public async Task<IActionResult> Index(int courseId)
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            var course = await _db.Courses
                .Include(c => c.Assignments)
                    .ThenInclude(a => a.Submissions)
                        .ThenInclude(s => s.Student)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return NotFound();

            ViewBag.Course   = course;
            ViewBag.UserId   = SessionHelper.GetUserId(HttpContext);
            ViewBag.UserRole = SessionHelper.GetUserRole(HttpContext);

            return View(course.Assignments.ToList());
        }

        // ── GET /Assignment/Create?courseId=5 ────────────────────────
        public async Task<IActionResult> Create(int courseId)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            ViewBag.CourseId = courseId;
            ViewBag.CourseName = (await _db.Courses.FindAsync(courseId))?.Title;
            return View();
        }

        // ── POST /Assignment/Create ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int courseId, string title,
            string description, DateTime dueDate, int maxMarks)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Title is required.";
                return RedirectToAction("Create", new { courseId });
            }

            _db.Assignments.Add(new Assignment
            {
                CourseId    = courseId,
                Title       = title,
                Description = description,
                DueDate     = dueDate,
                MaxMarks    = maxMarks
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Assignment created.";
            return RedirectToAction("Index", new { courseId });
        }

        // ── GET /Assignment/Submit/5 — Student submission form ───────
        public async Task<IActionResult> Submit(int id)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Student"))
                return RedirectToAction("Login", "Auth");

            var assignment = await _db.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null) return NotFound();

            int userId = SessionHelper.GetUserId(HttpContext);

            // Check if already submitted
            ViewBag.Existing = await _db.AssignmentSubmissions
                .FirstOrDefaultAsync(s => s.AssignmentId == id && s.StudentId == userId);

            return View(assignment);
        }

        // ── POST /Assignment/Submit ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int assignmentId, string? comment,
            IFormFile? file)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Student"))
                return RedirectToAction("Login", "Auth");

            int userId = SessionHelper.GetUserId(HttpContext);

            // Prevent double submission
            bool alreadySubmitted = await _db.AssignmentSubmissions
                .AnyAsync(s => s.AssignmentId == assignmentId && s.StudentId == userId);

            if (alreadySubmitted)
            {
                TempData["Error"] = "You have already submitted this assignment.";
                return RedirectToAction("Submit", new { id = assignmentId });
            }

            string? fileUrl = null;
            if (file != null && file.Length > 0)
            {
                string dir = Path.Combine(_env.WebRootPath, "uploads", "submissions");
                Directory.CreateDirectory(dir);
                string name = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                using var stream = new FileStream(Path.Combine(dir, name), FileMode.Create);
                await file.CopyToAsync(stream);
                fileUrl = $"/uploads/submissions/{name}";
            }

            string isLate = DateTime.Now > (await _db.Assignments
                .Where(a => a.Id == assignmentId)
                .Select(a => a.DueDate)
                .FirstAsync()) ? "Late" : "Submitted";

            _db.AssignmentSubmissions.Add(new AssignmentSubmission
            {
                AssignmentId = assignmentId,
                StudentId    = userId,
                FileUrl      = fileUrl,
                Comment      = comment,
                Status       = isLate
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = "Submission received.";
            int courseId = (await _db.Assignments.FindAsync(assignmentId))!.CourseId;
            return RedirectToAction("Index", new { courseId });
        }

        // ── GET /Assignment/Grade/submissionId — Educator grades ─────
        public async Task<IActionResult> Grade(int submissionId)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var sub = await _db.AssignmentSubmissions
                .Include(s => s.Assignment)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (sub == null) return NotFound();
            return View(sub);
        }

        // ── POST /Assignment/Grade ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(int submissionId, int marks, string feedback)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var sub = await _db.AssignmentSubmissions
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (sub == null) return NotFound();

            sub.MarksAwarded   = marks;
            sub.GraderFeedback = feedback;
            sub.Status         = "Graded";
            sub.GradedAt       = DateTime.Now;

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Graded {sub.Student?.FullName ?? "student"}: {marks} marks.";
            return RedirectToAction("Index", new { courseId = sub.Assignment.CourseId });
        }
    }
}
