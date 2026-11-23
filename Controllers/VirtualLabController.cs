using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PathForge.Data;
using PathForge.Helpers;
using PathForge.Models.Entities;
using PathForge.Models.ViewModels;

namespace PathForge.Controllers
{
    /// <summary>
    /// Virtual Lab — a browser-based HTML/CSS/JS sandbox attached to a module.
    /// Educators author a lab; students code against it with a live preview and
    /// an automated marker check that completes the module when passed.
    /// </summary>
    public class VirtualLabController : Controller
    {
        private readonly PathForgeDbContext _db;
        public VirtualLabController(PathForgeDbContext db) { _db = db; }

        // ── GET /VirtualLab/Create?moduleId=5 — Educator authoring ──
        public async Task<IActionResult> Create(int moduleId)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var module = await _db.Modules
                .Include(m => m.Course)
                .Include(m => m.Lab)
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module == null) return NotFound();
            if (!CanManage(module.Course)) return Forbid();

            // One lab per module — if it already exists, edit it instead.
            if (module.Lab != null)
                return RedirectToAction("Edit", new { id = module.Lab.Id });

            return View(new VirtualLabViewModel
            {
                ModuleId    = moduleId,
                CourseId    = module.CourseId,
                Title       = module.Title,
                StarterHtml = "<!-- Write your HTML here -->\n<h1>Hello, PathForge</h1>",
                StarterCss  = "/* Write your CSS here */\nh1 { color: #d8000f; }",
                StarterJs   = "// Write your JavaScript here",
                ExpectedMarkers = "<h1\ncolor"
            });
        }

        // ── POST /VirtualLab/Create ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VirtualLabViewModel model)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var module = await _db.Modules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.Id == model.ModuleId);

            if (module == null) return NotFound();
            if (!CanManage(module.Course)) return Forbid();
            if (!ModelState.IsValid) return View(model);

            // Make sure the hosting module is flagged as a Lab.
            module.Type = "Lab";

            _db.VirtualLabs.Add(new VirtualLab
            {
                ModuleId        = model.ModuleId,
                Title           = model.Title,
                Instructions    = model.Instructions,
                StarterHtml     = model.StarterHtml ?? "",
                StarterCss      = model.StarterCss ?? "",
                StarterJs       = model.StarterJs ?? "",
                ExpectedMarkers = model.ExpectedMarkers
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = "Virtual lab created.";
            return RedirectToAction("EditModules", "Course", new { id = module.CourseId });
        }

        // ── GET /VirtualLab/Edit/5 — Educator editing ───────────────
        public async Task<IActionResult> Edit(int id)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var lab = await _db.VirtualLabs
                .Include(l => l.Module).ThenInclude(m => m.Course)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lab == null) return NotFound();
            if (!CanManage(lab.Module.Course)) return Forbid();

            return View("Create", new VirtualLabViewModel
            {
                Id              = lab.Id,
                ModuleId        = lab.ModuleId,
                CourseId        = lab.Module.CourseId,
                Title           = lab.Title,
                Instructions    = lab.Instructions,
                StarterHtml     = lab.StarterHtml,
                StarterCss      = lab.StarterCss,
                StarterJs       = lab.StarterJs,
                ExpectedMarkers = lab.ExpectedMarkers
            });
        }

        // ── POST /VirtualLab/Edit/5 ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VirtualLabViewModel model)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var lab = await _db.VirtualLabs
                .Include(l => l.Module).ThenInclude(m => m.Course)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lab == null) return NotFound();
            if (!CanManage(lab.Module.Course)) return Forbid();
            if (!ModelState.IsValid) return View("Create", model);

            lab.Title           = model.Title;
            lab.Instructions    = model.Instructions;
            lab.StarterHtml     = model.StarterHtml ?? "";
            lab.StarterCss      = model.StarterCss ?? "";
            lab.StarterJs       = model.StarterJs ?? "";
            lab.ExpectedMarkers = model.ExpectedMarkers;
            lab.UpdatedAt       = DateTime.Now;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Virtual lab updated.";
            return RedirectToAction("EditModules", "Course", new { id = lab.Module.CourseId });
        }

        // ── POST /VirtualLab/Delete/5 ───────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var lab = await _db.VirtualLabs
                .Include(l => l.Module).ThenInclude(m => m.Course)
                .Include(l => l.Submissions)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lab == null) return NotFound();
            if (!CanManage(lab.Module.Course)) return Forbid();

            int courseId = lab.Module.CourseId;
            _db.VirtualLabSubmissions.RemoveRange(lab.Submissions);
            _db.VirtualLabs.Remove(lab);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Virtual lab deleted.";
            return RedirectToAction("EditModules", "Course", new { id = courseId });
        }

        // ── GET /VirtualLab/Workspace/5 — Student (and preview) ─────
        public async Task<IActionResult> Workspace(int id)
        {
            var lab = await _db.VirtualLabs
                .Include(l => l.Module).ThenInclude(m => m.Course).ThenInclude(c => c.Educator)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lab == null) return NotFound();

            int userId      = SessionHelper.GetUserId(HttpContext);
            string role     = SessionHelper.GetUserRole(HttpContext);
            bool isManager  = role == "Admin" || lab.Module.Course.EducatorId == userId;

            if (userId == 0)
            {
                TempData["Error"] = "Please log in to open the lab.";
                return RedirectToAction("Login", "Auth");
            }

            bool enrolled = await _db.CourseEnrolments
                .AnyAsync(e => e.CourseId == lab.Module.CourseId && e.StudentId == userId);

            if (!enrolled && !isManager)
            {
                TempData["Error"] = "Enrol in the course to open this lab.";
                return RedirectToAction("Details", "Course", new { id = lab.Module.CourseId });
            }

            var submission = await _db.VirtualLabSubmissions
                .FirstOrDefaultAsync(s => s.VirtualLabId == id && s.StudentId == userId);

            ViewBag.Submission = submission;
            ViewBag.IsManager  = isManager;
            return View(lab);
        }

        // ── POST /VirtualLab/Save — Save work + run marker checks ───
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(VirtualLabWorkViewModel model)
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            var lab = await _db.VirtualLabs
                .Include(l => l.Module)
                .FirstOrDefaultAsync(l => l.Id == model.LabId);

            if (lab == null) return NotFound();

            int userId = SessionHelper.GetUserId(HttpContext);

            bool enrolled = await _db.CourseEnrolments
                .AnyAsync(e => e.CourseId == lab.Module.CourseId && e.StudentId == userId);
            if (!enrolled) return Forbid();

            // Run the automated check: every expected marker must appear
            // (case-insensitively) somewhere in the combined code.
            var markers = (lab.ExpectedMarkers ?? "")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            string combined = ((model.Html ?? "") + "\n" + (model.Css ?? "") + "\n" + (model.Js ?? ""))
                .ToLowerInvariant();

            int passed = markers.Count(m => combined.Contains(m.ToLowerInvariant()));
            bool allPass = markers.Count == 0 ? true : passed == markers.Count;

            var submission = await _db.VirtualLabSubmissions
                .FirstOrDefaultAsync(s => s.VirtualLabId == model.LabId && s.StudentId == userId);

            if (submission == null)
            {
                submission = new VirtualLabSubmission
                {
                    VirtualLabId = model.LabId,
                    StudentId    = userId
                };
                _db.VirtualLabSubmissions.Add(submission);
            }

            submission.Html         = model.Html ?? "";
            submission.Css          = model.Css ?? "";
            submission.Js           = model.Js ?? "";
            submission.ChecksPassed = passed;
            submission.ChecksTotal  = markers.Count;
            submission.UpdatedAt    = DateTime.Now;

            if (allPass && submission.Status != "Passed")
            {
                submission.Status   = "Passed";
                submission.PassedAt = DateTime.Now;
                await MarkModuleCompleteAsync(userId, lab.ModuleId);
                await IssueCertificateIfComplete(userId, lab.Module.CourseId);
            }

            await _db.SaveChangesAsync();

            if (allPass)
                TempData["Success"] = "All checks passed — the lab module is complete.";
            else
                TempData["Error"] = $"Saved. {passed} of {markers.Count} checks passed — keep going.";

            return RedirectToAction("Workspace", new { id = model.LabId });
        }

        // ── Helpers ──────────────────────────────────────────────────

        private bool CanManage(Course course)
        {
            int userId  = SessionHelper.GetUserId(HttpContext);
            string role = SessionHelper.GetUserRole(HttpContext);
            return role == "Admin" || course.EducatorId == userId;
        }

        // Reuses the same unlock logic as ProgressController.MarkComplete.
        private async Task MarkModuleCompleteAsync(int userId, int moduleId)        {
            var progress = await _db.StudentProgress
                .FirstOrDefaultAsync(p => p.StudentId == userId && p.ModuleId == moduleId);

            if (progress == null) return;
            if (progress.Status == "Completed") return;

            progress.Status      = "Completed";
            progress.CompletedAt = DateTime.Now;
            progress.UpdatedAt   = DateTime.Now;

            var module = await _db.Modules
                .Include(m => m.Course).ThenInclude(c => c.Modules)
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            var nextModule = module?.Course.Modules
                .FirstOrDefault(m => m.DisplayOrder == module.DisplayOrder + 1);

            if (nextModule != null)
            {
                var nextProgress = await _db.StudentProgress
                    .FirstOrDefaultAsync(p => p.StudentId == userId && p.ModuleId == nextModule.Id);
                if (nextProgress != null && nextProgress.Status == "Locked")
                {
                    nextProgress.Status    = "InProgress";
                    nextProgress.StartedAt = DateTime.Now;
                    nextProgress.UpdatedAt = DateTime.Now;
                }
            }
        }

        // Issues a course completion certificate once every module is complete.
        private async Task IssueCertificateIfComplete(int userId, int courseId)
        {
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
