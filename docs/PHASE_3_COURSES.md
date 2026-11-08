# PHASE 3 — COURSE MANAGEMENT, CONTENT & ENROLMENT
### PathForge · Week 3 of 5

> **Prerequisite:** Phase 2 complete. Login/session works correctly.
> This is the heaviest week. Break each section into one sitting.

---

## WHAT YOU BUILD THIS WEEK

- Course catalogue (public browsing — no login required)
- Course detail page (shows modules and materials)
- Educator: create, edit, publish, delete courses
- Educator: add modules and upload materials (files)
- Student: enrol in a course and see enrolled courses
- Admin: see all courses with status management

---

## STEP 1 — COURSE VIEW MODELS

**`Models/ViewModels/CourseViewModel.cs`**
```csharp
using System.ComponentModel.DataAnnotations;

namespace PathForge.Models.ViewModels
{
    public class CourseViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        // Thumbnail file (optional upload)
        public IFormFile? ThumbnailFile { get; set; }
        public string? ExistingThumbnailUrl { get; set; }
    }

    public class ModuleViewModel
    {
        public int Id { get; set; }
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Module title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string Type { get; set; } = "Lesson";  // Lesson | Video | Lab | Reading

        public int DisplayOrder { get; set; }
    }

    public class MaterialViewModel
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }

        [Required(ErrorMessage = "Material title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a file type")]
        public string Type { get; set; } = "Document";  // Document | Video | Link | Slide

        // File upload — required only for non-Link types
        public IFormFile? File { get; set; }

        // For links — direct URL
        public string? LinkUrl { get; set; }
    }
}
```

---

## STEP 2 — COURSE CONTROLLER

**`Controllers/CourseController.cs`**
```csharp
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

            return View(await query.ToListAsync());
        }

        // ── GET /Course/Details/5 — Course detail + modules ─────────
        public async Task<IActionResult> Details(int id)
        {
            var course = await _db.Courses
                .Include(c => c.Educator)
                .Include(c => c.Modules.OrderBy(m => m.DisplayOrder))
                    .ThenInclude(m => m.Materials)
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
                Title       = model.Title,
                Description = model.Description,
                Category    = model.Category,
                ThumbnailUrl= thumbUrl,
                Status      = "Draft",
                EducatorId  = SessionHelper.GetUserId(HttpContext)
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
                Id                  = course.Id,
                Title               = course.Title,
                Description         = course.Description,
                Category            = course.Category,
                ExistingThumbnailUrl= course.ThumbnailUrl
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
```

---

## STEP 3 — COURSE CATALOGUE VIEW

**`Views/Course/Index.cshtml`**
```html
@model IEnumerable<PathForge.Models.Entities.Course>
@{
    ViewData["Title"] = "Course Catalogue";
    Layout = "_Layout";
}

<div class="pf-page" style="padding-top:3rem; padding-bottom:3rem;">

  {{!-- Header --}}
  <div style="display:flex; align-items:flex-start; justify-content:space-between;
              border-bottom:var(--border-outer); padding-bottom:1.5rem; margin-bottom:2.5rem;
              flex-wrap:wrap; gap:1rem;">
    <div>
      <span class="pf-label">All Courses</span>
      <h1 class="pf-section-header" style="margin-top:0.4rem;">Course Catalogue</h1>
    </div>

    {{!-- Search form --}}
    <form method="get" asp-action="Index" style="display:flex; gap:0.5rem; align-items:flex-end;">
      <div>
        <label class="pf-form-label">Search</label>
        <input type="text" name="search" value="@ViewBag.Search"
               class="pf-input" style="min-width:220px;" placeholder="Course title..." />
      </div>
      <button type="submit" class="pf-btn">Search</button>
      @if (!string.IsNullOrEmpty(ViewBag.Search?.ToString()))
      {
        <a href="/Course" class="pf-btn pf-btn--ghost">Clear</a>
      }
    </form>
  </div>

  {{!-- Category filter pills --}}
  @if (ViewBag.Categories != null)
  {
    <div style="display:flex; gap:0.5rem; flex-wrap:wrap; margin-bottom:2rem;">
      <a href="/Course" class="pf-status @(ViewBag.CurrentCategory == null ? "pf-status--live" : "pf-status--draft")"
         style="text-decoration:none; padding:0.3rem 0.75rem; cursor:pointer;">
        All
      </a>
      @foreach (string cat in ViewBag.Categories)
      {
        <a href="/Course?category=@cat"
           class="pf-status @(ViewBag.CurrentCategory == cat ? "pf-status--live" : "pf-status--draft")"
           style="text-decoration:none; padding:0.3rem 0.75rem; cursor:pointer;">
          @cat
        </a>
      }
    </div>
  }

  {{!-- Course grid --}}
  @if (!Model.Any())
  {
    <div class="pf-card pf-card--outlined" style="padding:2rem;">
      <p class="pf-body">No courses found. Try a different search or category.</p>
    </div>
  }
  else
  {
    <div style="display:grid; grid-template-columns:repeat(auto-fill, minmax(300px, 1fr)); gap:1.5rem;">
      @foreach (var course in Model)
      {
        <div class="pf-course-card">
          @if (!string.IsNullOrEmpty(course.ThumbnailUrl))
          {
            <img src="@course.ThumbnailUrl" alt="@course.Title"
                 style="width:100%; height:160px; object-fit:cover; border-bottom:var(--border-inner);" />
          }
          else
          {
            {{!-- Placeholder block in light background --}}
            <div style="width:100%; height:120px; background:var(--light);
                        border-bottom:var(--border-inner);
                        display:flex; align-items:center; justify-content:center;">
              <span class="pf-label">@course.Category</span>
            </div>
          }

          <div style="padding:1rem 1.25rem;">
            <span class="pf-course-card__category">@course.Category</span>
            <h2 class="pf-course-card__title" style="margin-top:0.35rem;">@course.Title</h2>
            <p class="pf-body-cell" style="margin-top:0.5rem; opacity:0.65;
               overflow:hidden; display:-webkit-box; -webkit-line-clamp:2; -webkit-box-orient:vertical;">
              @course.Description
            </p>

            <div class="pf-course-card__meta" style="margin-top:0.75rem;">
              @course.Enrolments.Count enrolled
              &nbsp;·&nbsp; By @course.Educator.FullName
            </div>

            <div class="pf-course-card__actions">
              <a asp-action="Details" asp-route-id="@course.Id" class="pf-btn pf-btn--sm">
                View Course
              </a>
            </div>
          </div>
        </div>
      }
    </div>
  }

</div>
```

---

## STEP 4 — COURSE DETAIL VIEW

**`Views/Course/Details.cshtml`**
```html
@model PathForge.Models.Entities.Course
@{
    ViewData["Title"] = Model.Title;
    Layout = "_Layout";
    bool isEnrolled = ViewBag.IsEnrolled == true;
    bool isOwner    = ViewBag.IsOwner == true;
    string userRole = ViewBag.UserRole ?? "";
}

<div class="pf-page" style="padding-top:3rem; padding-bottom:3rem;">

  {{!-- Hero row --}}
  <div style="display:grid; grid-template-columns:1fr auto; gap:2rem;
              align-items:start; border-bottom:var(--border-outer);
              padding-bottom:2rem; margin-bottom:2.5rem;">
    <div>
      <span class="pf-label">@Model.Category</span>
      <h1 class="pf-section-header" style="margin-top:0.4rem;">@Model.Title</h1>
      <p class="pf-body" style="margin-top:0.75rem; max-width:600px;">@Model.Description</p>
      <p class="pf-body" style="margin-top:0.5rem; font-size:0.8rem; opacity:0.6;">
        By @Model.Educator.FullName &nbsp;·&nbsp;
        @Model.Enrolments.Count enrolled &nbsp;·&nbsp;
        <span class="pf-status pf-status--@Model.Status.ToLower()">@Model.Status</span>
      </p>
    </div>

    <div style="display:flex; flex-direction:column; gap:0.75rem; min-width:160px;">
      @if (isOwner || userRole == "Admin")
      {
        <a asp-action="EditModules" asp-route-id="@Model.Id" class="pf-btn pf-btn--ghost pf-btn--sm">
          Manage Modules
        </a>
        <a asp-action="Edit" asp-route-id="@Model.Id" class="pf-btn pf-btn--ghost pf-btn--sm">
          Edit Course
        </a>
      }
      else if (userRole == "Student" && !isEnrolled)
      {
        <form asp-action="Enrol" asp-route-id="@Model.Id" method="post">
          @Html.AntiForgeryToken()
          <button type="submit" class="pf-btn" style="width:100%;">Enrol Now</button>
        </form>
      }
      else if (isEnrolled)
      {
        <a asp-controller="Progress" asp-action="Index" class="pf-btn">
          My Progress
        </a>
      }
    </div>
  </div>

  {{!-- Module list --}}
  <h2 class="pf-card-title" style="margin-bottom:1.5rem;">Course Modules</h2>

  @if (!Model.Modules.Any())
  {
    <div class="pf-alert">No modules added yet.</div>
  }
  else
  {
    <div style="display:flex; flex-direction:column; gap:1rem;">
      @foreach (var module in Model.Modules)
      {
        <div class="pf-card pf-card--outlined">
          <div style="display:flex; align-items:center; justify-content:space-between;">
            <div>
              <span class="pf-label" style="margin-bottom:0.25rem;">
                Module @module.DisplayOrder &nbsp;·&nbsp; @module.Type
              </span>
              <h3 class="pf-card-title" style="font-size:1.1rem;">@module.Title</h3>
              @if (!string.IsNullOrEmpty(module.Description))
              {
                <p class="pf-body-cell" style="margin-top:0.25rem; opacity:0.7;">
                  @module.Description
                </p>
              }
            </div>
            <div style="text-align:right; flex-shrink:0; padding-left:1rem;">
              <span class="pf-stat-cell__number" style="font-size:1.25rem;">
                @module.Materials.Count
              </span>
              <span class="pf-stat-cell__label">Resources</span>
            </div>
          </div>

          {{!-- Materials for enrolled students --}}
          @if (isEnrolled || isOwner || userRole == "Admin")
          {
            @if (module.Materials.Any())
            {
              <div style="border-top:var(--border-inner); margin-top:1rem; padding-top:0.75rem;">
                <ul style="list-style:none; display:flex; flex-direction:column; gap:0.4rem;">
                  @foreach (var mat in module.Materials)
                  {
                    <li style="display:flex; align-items:center; gap:0.75rem;">
                      <span class="pf-label" style="min-width:70px;">@mat.Type</span>
                      <a href="@mat.FileUrl" target="_blank" class="pf-body-cell"
                         style="color:var(--dark); text-decoration:none; border-bottom:1px solid var(--red);">
                        @mat.Title
                      </a>
                    </li>
                  }
                </ul>
              </div>
            }
          }
          else
          {
            <p class="pf-body-cell" style="margin-top:0.75rem; opacity:0.5;">
              Enrol to access resources
            </p>
          }
        </div>
      }
    </div>
  }

</div>
```

---

## STEP 5 — EDUCATOR MODULE MANAGEMENT VIEW

**`Views/Course/EditModules.cshtml`**
```html
@model PathForge.Models.Entities.Course
@{
    ViewData["Title"] = "Manage Modules — " + Model.Title;
    Layout = "_Layout";
}

<div class="pf-page" style="padding-top:3rem; padding-bottom:3rem;">

  <div style="border-bottom:var(--border-outer); padding-bottom:1.5rem; margin-bottom:2rem;">
    <span class="pf-label">Course Editor</span>
    <h1 class="pf-section-header" style="margin-top:0.4rem;">@Model.Title</h1>
    <div style="display:flex; gap:0.75rem; margin-top:1rem; flex-wrap:wrap;">
      <a asp-action="Details" asp-route-id="@Model.Id" class="pf-btn pf-btn--ghost pf-btn--sm">
        Preview
      </a>
      <a asp-action="Edit" asp-route-id="@Model.Id" class="pf-btn pf-btn--ghost pf-btn--sm">
        Edit Details
      </a>
      <a asp-action="Manage" class="pf-btn pf-btn--ghost pf-btn--sm">
        ← All My Courses
      </a>
    </div>
  </div>

  @if (TempData["Success"] != null)
  {
    <div class="pf-alert pf-alert--success" style="margin-bottom:1.5rem;">@TempData["Success"]</div>
  }
  @if (TempData["Error"] != null)
  {
    <div class="pf-alert pf-alert--error" style="margin-bottom:1.5rem;">@TempData["Error"]</div>
  }

  <div style="display:grid; grid-template-columns:1fr 360px; gap:3rem; align-items:start;">

    {{!-- Module list --}}
    <div>
      <h2 class="pf-card-title" style="margin-bottom:1.25rem;">
        Modules (@Model.Modules.Count())
      </h2>

      @foreach (var module in Model.Modules)
      {
        <div class="pf-card pf-card--outlined" style="margin-bottom:1rem;">
          <div style="display:flex; justify-content:space-between; align-items:start;">
            <div>
              <span class="pf-label">@module.Type · Order @module.DisplayOrder</span>
              <h3 class="pf-card-title" style="font-size:1.05rem; margin-top:0.2rem;">
                @module.Title
              </h3>
            </div>
            <form asp-action="DeleteModule" method="post" style="margin:0;">
              @Html.AntiForgeryToken()
              <input type="hidden" name="moduleId" value="@module.Id" />
              <button type="submit" class="pf-btn pf-btn--danger pf-btn--sm"
                      onclick="return confirm('Delete this module?')">
                Delete
              </button>
            </form>
          </div>

          {{!-- Materials --}}
          @if (module.Materials.Any())
          {
            <div style="border-top:var(--border-inner); margin-top:0.75rem; padding-top:0.75rem;">
              @foreach (var mat in module.Materials)
              {
                <div style="display:flex; align-items:center; gap:0.5rem; padding:0.25rem 0;">
                  <span class="pf-label">@mat.Type</span>
                  <span class="pf-body-cell">@mat.Title</span>
                </div>
              }
            </div>
          }

          {{!-- Upload material form --}}
          <details style="margin-top:0.75rem;">
            <summary class="pf-label" style="cursor:pointer; user-select:none;">
              + Add Resource
            </summary>
            <form asp-action="UploadMaterial" method="post" enctype="multipart/form-data"
                  style="margin-top:0.75rem; padding-top:0.75rem; border-top:var(--border-inner);">
              @Html.AntiForgeryToken()
              <input type="hidden" name="ModuleId" value="@module.Id" />

              <div class="pf-form-group">
                <label class="pf-form-label">Resource Title</label>
                <input type="text" name="Title" class="pf-input" required
                       placeholder="e.g. Week 1 Slides" />
              </div>
              <div class="pf-form-group">
                <label class="pf-form-label">Type</label>
                <select name="Type" class="pf-input pf-select" id="mat-type-@module.Id"
                        onchange="toggleLinkField(this, @module.Id)">
                  <option value="Document">Document</option>
                  <option value="Slide">Slide</option>
                  <option value="Video">Video</option>
                  <option value="Link">External Link</option>
                </select>
              </div>
              <div class="pf-form-group" id="file-field-@module.Id">
                <label class="pf-form-label">Upload File</label>
                <input type="file" name="File" class="pf-input" />
              </div>
              <div class="pf-form-group" id="link-field-@module.Id" style="display:none;">
                <label class="pf-form-label">URL</label>
                <input type="url" name="LinkUrl" class="pf-input" placeholder="https://..." />
              </div>
              <button type="submit" class="pf-btn pf-btn--sm">Upload</button>
            </form>
          </details>
        </div>
      }
    </div>

    {{!-- Add module panel --}}
    <div style="position:sticky; top:80px;">
      <h2 class="pf-card-title" style="margin-bottom:1.25rem;">Add Module</h2>
      <div class="pf-card pf-card--outlined">
        <form asp-action="AddModule" method="post">
          @Html.AntiForgeryToken()
          <input type="hidden" name="CourseId" value="@Model.Id" />

          <div class="pf-form-group">
            <label class="pf-form-label">Module Title</label>
            <input type="text" name="Title" class="pf-input" required
                   placeholder="e.g. Introduction to HTML" />
          </div>
          <div class="pf-form-group">
            <label class="pf-form-label">Description (optional)</label>
            <textarea name="Description" class="pf-input pf-textarea"
                      style="min-height:80px;"></textarea>
          </div>
          <div class="pf-form-group">
            <label class="pf-form-label">Type</label>
            <select name="Type" class="pf-input pf-select">
              <option value="Lesson">Lesson</option>
              <option value="Video">Video</option>
              <option value="Lab">Lab</option>
              <option value="Reading">Reading</option>
            </select>
          </div>
          <button type="submit" class="pf-btn" style="width:100%;">Add Module</button>
        </form>
      </div>
    </div>

  </div>
</div>

@section Scripts {
<script>
function toggleLinkField(select, moduleId) {
  var isLink     = select.value === 'Link';
  var fileField  = document.getElementById('file-field-' + moduleId);
  var linkField  = document.getElementById('link-field-' + moduleId);
  if (fileField) fileField.style.display = isLink ? 'none' : '';
  if (linkField) linkField.style.display = isLink ? '' : 'none';
}
</script>
}
```

---

## STEP 6 — EDUCATOR MANAGE VIEW

**`Views/Course/Manage.cshtml`**
```html
@model IEnumerable<PathForge.Models.Entities.Course>
@{
    ViewData["Title"] = "Manage Courses";
    Layout = "_Layout";
}

<div class="pf-page" style="padding-top:3rem; padding-bottom:3rem;">

  <div style="display:flex; justify-content:space-between; align-items:flex-start;
              border-bottom:var(--border-outer); padding-bottom:1.5rem; margin-bottom:2rem;">
    <div>
      <span class="pf-label">Educator Portal</span>
      <h1 class="pf-section-header" style="margin-top:0.4rem;">My Courses</h1>
    </div>
    <a asp-action="Create" class="pf-btn">+ New Course</a>
  </div>

  @if (TempData["Success"] != null)
  {
    <div class="pf-alert pf-alert--success" style="margin-bottom:1.5rem;">@TempData["Success"]</div>
  }

  <div class="pf-table-wrap">
    <table class="pf-table">
      <thead>
        <tr>
          <th>Course Title</th>
          <th>Category</th>
          <th>Enrolled</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        @foreach (var course in Model)
        {
          <tr>
            <td>
              <a asp-action="Details" asp-route-id="@course.Id"
                 style="color:var(--dark); border-bottom:1px solid var(--red);">
                @course.Title
              </a>
            </td>
            <td>@course.Category</td>
            <td class="cell-number">@course.Enrolments.Count</td>
            <td><span class="pf-status pf-status--@course.Status.ToLower()">@course.Status</span></td>
            <td>
              <div style="display:flex; gap:0.4rem; flex-wrap:wrap;">
                <a asp-action="EditModules" asp-route-id="@course.Id"
                   class="pf-btn pf-btn--ghost pf-btn--sm">Modules</a>

                @if (course.Status == "Draft")
                {
                  <form asp-action="SetStatus" method="post" style="display:inline;">
                    @Html.AntiForgeryToken()
                    <input type="hidden" name="id"     value="@course.Id" />
                    <input type="hidden" name="status" value="Live" />
                    <button type="submit" class="pf-btn pf-btn--sm">Publish</button>
                  </form>
                }
                else if (course.Status == "Live")
                {
                  <form asp-action="SetStatus" method="post" style="display:inline;">
                    @Html.AntiForgeryToken()
                    <input type="hidden" name="id"     value="@course.Id" />
                    <input type="hidden" name="status" value="Draft" />
                    <button type="submit" class="pf-btn pf-btn--ghost pf-btn--sm">Unpublish</button>
                  </form>
                }

                <form asp-action="Delete" method="post" style="display:inline;">
                  @Html.AntiForgeryToken()
                  <input type="hidden" name="id" value="@course.Id" />
                  <button type="submit" class="pf-btn pf-btn--danger pf-btn--sm"
                          onclick="return confirm('Delete @course.Title?')">Delete</button>
                </form>
              </div>
            </td>
          </tr>
        }
      </tbody>
    </table>
  </div>

</div>
```

---

## WEEK 3 DONE — CHECKLIST

- [ ] Educator can create, edit, publish, unpublish, and delete courses
- [ ] Educator can add and delete modules
- [ ] Educator can upload files (Document, Slide, Video) and add links
- [ ] Student can browse the course catalogue with category filter and search
- [ ] Student can enrol in a course — progress records created automatically
- [ ] Enrolled student sees materials; non-enrolled sees lock message
- [ ] Files upload to `wwwroot/uploads/materials/` with unique names
- [ ] Non-educator trying to access `/Course/Manage` is blocked

---

## WHAT PHASE 4 COVERS

Phase 4 builds Assignments, Quizzes, the Discussion Forum,
the Learning Path Constellation, and the Admin panel.
