using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PathForge.Data;
using PathForge.Helpers;
using PathForge.Models.Entities;
using PathForge.Models.ViewModels;

namespace PathForge.Controllers
{
    public class CourseController : Controller
    {
        private readonly PathForgeDbContext _db;
        private readonly IWebHostEnvironment _env;

        public CourseController(PathForgeDbContext db, IWebHostEnvironment env)
        {
            _db  = db;
            _env = env;
        }

        // ── GET /Course — Public course catalogue ───────────────────
        public async Task<IActionResult> Index(string? category, string? search)
        {
            var query = _db.Courses
                .Include(c => c.Educator)
                .Include(c => c.Enrolments)
                .Where(c => c.Status == "Live")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(c => c.Category == category);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c =>
                    c.Title.Contains(search) ||
                    c.Description.Contains(search));

            ViewBag.Categories = await _db.Courses
                .Where(c => c.Status == "Live")
                .Select(c => c.Category)
                .Distinct()
                .ToListAsync();

            ViewBag.CurrentCategory = category;
            ViewBag.Search = search;

            // For logged-in students: expose their per-course progress so the
            // catalogue cards can show a completion bar.
            int userId = SessionHelper.GetUserId(HttpContext);
            if (userId > 0 && SessionHelper.GetUserRole(HttpContext) == "Student")
            {
                var enrolledIds = await _db.CourseEnrolments
                    .Where(e => e.StudentId == userId)
                    .Select(e => e.CourseId)
                    .ToListAsync();

                var progress = await _db.StudentProgress
                    .Where(p => p.StudentId == userId)
                    .Select(p => new { p.ModuleId, p.Status })
                    .ToListAsync();

                var moduleCourse = await _db.Modules
                    .Where(m => enrolledIds.Contains(m.CourseId))
                    .Select(m => new { m.Id, m.CourseId })
                    .ToListAsync();

                var pct = new Dictionary<int, int>();
                foreach (var cid in enrolledIds)
                {
                    var moduleIds = moduleCourse.Where(m => m.CourseId == cid).Select(m => m.Id).ToHashSet();
                    int tot = moduleIds.Count;
                    int done = progress.Count(p => moduleIds.Contains(p.ModuleId) && p.Status == "Completed");
                    pct[cid] = tot == 0 ? 0 : (int)Math.Round(100.0 * done / tot);
                }

                ViewBag.EnrolledIds = enrolledIds;
                ViewBag.ProgressPercent = pct;
            }

            return View(await query.ToListAsync());
        }

        // ── GET /Course/Details/5 — Course detail + modules ─────────
        public async Task<IActionResult> Details(int id)
        {
            var course = await _db.Courses
                .Include(c => c.Educator)
                .Include(c => c.Modules.OrderBy(m => m.DisplayOrder))
                    .ThenInclude(m => m.Materials)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lab)
                .Include(c => c.Enrolments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            // Check if current student is enrolled
            int userId = SessionHelper.GetUserId(HttpContext);
            ViewBag.IsEnrolled = userId > 0 &&
                await _db.CourseEnrolments
                    .AnyAsync(e => e.CourseId == id && e.StudentId == userId);

            ViewBag.IsOwner = userId == course.EducatorId;
            ViewBag.UserRole = SessionHelper.GetUserRole(HttpContext);

            return View(course);
        }

        // ── GET /Course/Manage — Educator's own courses ─────────────
        public async Task<IActionResult> Manage()
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            int userId = SessionHelper.GetUserId(HttpContext);
            string role = SessionHelper.GetUserRole(HttpContext);

            var query = _db.Courses
                .Include(c => c.Enrolments)
                .Include(c => c.Assignments)
                    .ThenInclude(a => a.Submissions)
                .AsQueryable();

            // Admins see all, educators see only their own
            if (role == "Educator")
                query = query.Where(c => c.EducatorId == userId);

            return View(await query.OrderByDescending(c => c.CreatedAt).ToListAsync());
        }

        // ── GET /Course/Create ───────────────────────────────────────
        public IActionResult Create()
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");
            return View(new CourseViewModel());
        }

        // ── POST /Course/Create ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseViewModel model)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid) return View(model);

            string? thumbUrl = null;
            if (model.ThumbnailFile != null && model.ThumbnailFile.Length > 0)
                thumbUrl = await SaveFile(model.ThumbnailFile, "thumbnails");

            var course = new Course
            {
                Title        = model.Title,
                Description  = model.Description,
                Category     = model.Category,
                ThumbnailUrl = thumbUrl,
                Status       = "Draft",
                EducatorId   = SessionHelper.GetUserId(HttpContext)
            };

            _db.Courses.Add(course);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Course created. Add modules to get started.";
            return RedirectToAction("EditModules", new { id = course.Id });
        }

        // ── GET /Course/Edit/5 ───────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var course = await _db.Courses.FindAsync(id);
            if (course == null) return NotFound();
            if (!CanManageCourse(course)) return Forbid();

            return View(new CourseViewModel
            {
                Id                   = course.Id,
                Title                = course.Title,
                Description          = course.Description,
                Category             = course.Category,
                ExistingThumbnailUrl = course.ThumbnailUrl
            });
        }

        // ── POST /Course/Edit/5 ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CourseViewModel model)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid) return View(model);

            var course = await _db.Courses.FindAsync(id);
            if (course == null) return NotFound();
            if (!CanManageCourse(course)) return Forbid();

            course.Title       = model.Title;
            course.Description = model.Description;
            course.Category    = model.Category;
            course.UpdatedAt   = DateTime.Now;

            if (model.ThumbnailFile != null && model.ThumbnailFile.Length > 0)
                course.ThumbnailUrl = await SaveFile(model.ThumbnailFile, "thumbnails");

            await _db.SaveChangesAsync();
            TempData["Success"] = "Course updated.";
            return RedirectToAction("Manage");
        }

        // ── POST /Course/SetStatus — Publish / Unpublish / Archive ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, string status)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var course = await _db.Courses.FindAsync(id);
            if (course == null) return NotFound();
            if (!CanManageCourse(course)) return Forbid();

            var allowed = new[] { "Draft", "Live", "Archived" };
            if (!allowed.Contains(status)) return BadRequest();

            course.Status    = status;
            course.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Course status set to {status}.";
            return RedirectToAction("Manage");
        }

        // ── POST /Course/Delete/5 ────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var course = await _db.Courses
                .Include(c => c.Modules).ThenInclude(m => m.Materials)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();
            if (!CanManageCourse(course)) return Forbid();

            _db.Courses.Remove(course);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Course deleted.";
            return RedirectToAction("Manage");
        }

        // ── GET /Course/EditModules/5 — Module management ───────────
        public async Task<IActionResult> EditModules(int id)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var course = await _db.Courses
                .Include(c => c.Modules.OrderBy(m => m.DisplayOrder))
                    .ThenInclude(m => m.Materials)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lab)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();
            if (!CanManageCourse(course)) return Forbid();

            return View(course);
        }

        // ── POST /Course/AddModule ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddModule(ModuleViewModel model)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var course = await _db.Courses.FindAsync(model.CourseId);
            if (course == null || !CanManageCourse(course)) return Forbid();

            int maxOrder = await _db.Modules
                .Where(m => m.CourseId == model.CourseId)
                .Select(m => (int?)m.DisplayOrder)
                .MaxAsync() ?? 0;

            var module = new Module
            {
                CourseId     = model.CourseId,
                Title        = model.Title,
                Description  = model.Description,
                Type         = model.Type,
                DisplayOrder = maxOrder + 1
            };

            _db.Modules.Add(module);
            await _db.SaveChangesAsync();

            return RedirectToAction("EditModules", new { id = model.CourseId });
        }

        // ── POST /Course/DeleteModule/5 ──────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteModule(int moduleId)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var module = await _db.Modules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module == null) return NotFound();
            if (!CanManageCourse(module.Course)) return Forbid();

            int courseId = module.CourseId;
            _db.Modules.Remove(module);
            await _db.SaveChangesAsync();

            return RedirectToAction("EditModules", new { id = courseId });
        }

        // ── POST /Course/UploadMaterial ──────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadMaterial(MaterialViewModel model)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var module = await _db.Modules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.Id == model.ModuleId);

            if (module == null || !CanManageCourse(module.Course)) return Forbid();

            string fileUrl;
            long fileSize = 0;

            if (model.Type == "Link" && !string.IsNullOrWhiteSpace(model.LinkUrl))
            {
                fileUrl = model.LinkUrl;
            }
            else if (model.File != null && model.File.Length > 0)
            {
                fileUrl  = await SaveFile(model.File, "materials");
                fileSize = model.File.Length;
            }
            else
            {
                TempData["Error"] = "Please provide a file or a URL.";
                return RedirectToAction("EditModules", new { id = module.CourseId });
            }

            var material = new Material
            {
                ModuleId      = model.ModuleId,
                Title         = model.Title,
                Type          = model.Type,
                FileUrl       = fileUrl,
                FileSizeBytes = fileSize
            };

            _db.Materials.Add(material);
            await _db.SaveChangesAsync();

            return RedirectToAction("EditModules", new { id = module.CourseId });
        }

        // ── POST /Course/Enrol/5 — Student enrolment ────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(int id)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Student"))
                return RedirectToAction("Login", "Auth");

            int userId = SessionHelper.GetUserId(HttpContext);

            bool alreadyEnrolled = await _db.CourseEnrolments
                .AnyAsync(e => e.CourseId == id && e.StudentId == userId);

            if (!alreadyEnrolled)
            {
                _db.CourseEnrolments.Add(new CourseEnrolment
                {
                    CourseId  = id,
                    StudentId = userId
                });

                // Auto-create Locked progress records for all modules
                var modules = await _db.Modules
                    .Where(m => m.CourseId == id)
                    .ToListAsync();

                foreach (var module in modules)
                {
                    _db.StudentProgress.Add(new StudentProgress
                    {
                        StudentId = userId,
                        ModuleId  = module.Id,
                        Status    = module.DisplayOrder == 1 ? "InProgress" : "Locked"
                    });
                }

                await _db.SaveChangesAsync();
                TempData["Success"] = "You are now enrolled. Start with Module 1.";
            }

            return RedirectToAction("Details", new { id });
        }

        // ── Helpers ──────────────────────────────────────────────────

        private bool CanManageCourse(Course course)
        {
            int userId = SessionHelper.GetUserId(HttpContext);
            string role = SessionHelper.GetUserRole(HttpContext);
            return role == "Admin" || course.EducatorId == userId;
        }

        private async Task<string> SaveFile(IFormFile file, string subfolder)
        {
            string uploadsDir = Path.Combine(
                _env.WebRootPath, "uploads", subfolder);
            Directory.CreateDirectory(uploadsDir);

            // Give the file a unique name to prevent overwrites
            string ext      = Path.GetExtension(file.FileName);
            string fileName = $"{Guid.NewGuid()}{ext}";
            string fullPath = Path.Combine(uploadsDir, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/{subfolder}/{fileName}";
        }
    }
}
