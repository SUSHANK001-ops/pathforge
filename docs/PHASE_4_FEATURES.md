# PHASE 4 — ASSIGNMENTS, QUIZZES, DISCUSSIONS, PROGRESS & ADMIN
### PathForge · Week 4 of 5

> **Prerequisite:** Phase 3 complete. Course enrolment and file upload working.
> Split this week into four sittings: Assignments → Quizzes → Discussions → Constellation + Admin.

---

## WHAT YOU BUILD THIS WEEK

- Assignment creation (Educator) and file submission (Student)
- Quiz creation (Educator) and answering (Student) with auto-scoring
- Discussion forum — threaded posts per course
- Learning Path Constellation — visual star progress map
- Student Progress page
- Admin Panel — user management and site overview

---

## PART A — ASSIGNMENTS

### A1 — Assignment Controller

**`Controllers/AssignmentController.cs`**
```csharp
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
```

### A2 — Assignment Views

**`Views/Assignment/Create.cshtml`**
```html
@{
    ViewData["Title"] = "Create Assignment";
    Layout = "_Layout";
}

<div class="pf-page" style="padding-top:3rem; padding-bottom:3rem;">
  <span class="pf-label">@ViewBag.CourseName</span>
  <h1 class="pf-section-header" style="margin-top:0.4rem; margin-bottom:2rem;">
    Create Assignment
  </h1>

  <div class="pf-form-panel" style="max-width:600px;">
    <form method="post" asp-action="Create">
      @Html.AntiForgeryToken()
      <input type="hidden" name="courseId" value="@ViewBag.CourseId" />

      <div class="pf-form-group">
        <label class="pf-form-label">Assignment Title</label>
        <input type="text" name="title" class="pf-input" required
               placeholder="e.g. Week 2 Lab Report" />
      </div>

      <div class="pf-form-group">
        <label class="pf-form-label">Description / Instructions</label>
        <textarea name="description" class="pf-input pf-textarea" rows="5"
                  placeholder="Describe the task and what students should submit..."></textarea>
      </div>

      <div style="display:grid; grid-template-columns:1fr 1fr; gap:1rem;">
        <div class="pf-form-group">
          <label class="pf-form-label">Due Date</label>
          <input type="datetime-local" name="dueDate" class="pf-input" required />
        </div>
        <div class="pf-form-group">
          <label class="pf-form-label">Max Marks</label>
          <input type="number" name="maxMarks" class="pf-input"
                 value="100" min="1" max="1000" required />
        </div>
      </div>

      <div style="display:flex; gap:0.75rem; margin-top:0.5rem;">
        <button type="submit" class="pf-btn">Create Assignment</button>
        <a asp-action="Index" asp-route-courseId="@ViewBag.CourseId"
           class="pf-btn pf-btn--ghost">Cancel</a>
      </div>
    </form>
  </div>
</div>
```

**`Views/Assignment/Submit.cshtml`**
```html
@model PathForge.Models.Entities.Assignment
@{
    ViewData["Title"] = "Submit — " + Model.Title;
    Layout = "_Layout";
    var existing = ViewBag.Existing as PathForge.Models.Entities.AssignmentSubmission;
}

<div class="pf-page" style="padding-top:3rem; padding-bottom:3rem;">
  <span class="pf-label">@Model.Course.Title</span>
  <h1 class="pf-section-header" style="margin-top:0.4rem;">@Model.Title</h1>

  <div class="pf-card pf-card--outlined" style="max-width:600px; margin-top:2rem; padding:1.5rem;">
    <div style="margin-bottom:1rem;">
      <p class="pf-body">@Model.Description</p>
      <p class="pf-body" style="margin-top:0.5rem; font-size:0.8rem; opacity:0.6;">
        Due: @Model.DueDate.ToString("dd MMM yyyy, HH:mm")
        &nbsp;·&nbsp; Max marks: @Model.MaxMarks
      </p>
    </div>

    @if (existing != null)
    {
      <div class="pf-alert pf-alert--success" style="margin-bottom:1rem;">
        Already submitted on @existing.SubmittedAt.ToString("dd MMM yyyy") —
        Status: <strong>@existing.Status</strong>
        @if (existing.MarksAwarded.HasValue)
        {
          <span> — Marks: <strong>@existing.MarksAwarded / @Model.MaxMarks</strong></span>
        }
      </div>
      @if (!string.IsNullOrEmpty(existing.GraderFeedback))
      {
        <p class="pf-body" style="font-size:0.85rem; border-left:var(--border-leftbar); padding-left:1rem;">
          Feedback: @existing.GraderFeedback
        </p>
      }
    }
    else
    {
      <form asp-action="Submit" method="post" enctype="multipart/form-data">
        @Html.AntiForgeryToken()
        <input type="hidden" name="assignmentId" value="@Model.Id" />

        <div class="pf-form-group">
          <label class="pf-form-label">Upload File (optional)</label>
          <input type="file" name="file" class="pf-input" />
        </div>

        <div class="pf-form-group">
          <label class="pf-form-label">Comment</label>
          <textarea name="comment" class="pf-input pf-textarea" rows="3"
                    placeholder="Add notes about your submission..."></textarea>
        </div>

        <button type="submit" class="pf-btn">Submit Assignment</button>
      </form>
    }
  </div>
</div>
```

---

## PART B — QUIZZES

### B1 — Quiz Controller

**`Controllers/QuizController.cs`**
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PathForge.Data;
using PathForge.Helpers;
using PathForge.Models.Entities;

namespace PathForge.Controllers
{
    public class QuizController : Controller
    {
        private readonly PathForgeDbContext _db;

        public QuizController(PathForgeDbContext db) { _db = db; }

        // ── GET /Quiz/Create?courseId=5 ──────────────────────────────
        public IActionResult Create(int courseId)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");
            ViewBag.CourseId = courseId;
            return View();
        }

        // ── POST /Quiz/Create ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int courseId, string title,
            string? description, int timeLimitMinutes, int passMarkPercent)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var quiz = new Quiz
            {
                CourseId         = courseId,
                Title            = title,
                Description      = description,
                TimeLimitMinutes = timeLimitMinutes,
                PassMarkPercent  = passMarkPercent
            };
            _db.Quizzes.Add(quiz);
            await _db.SaveChangesAsync();

            return RedirectToAction("EditQuestions", new { id = quiz.Id });
        }

        // ── GET /Quiz/EditQuestions/5 — Add questions ────────────────
        public async Task<IActionResult> EditQuestions(int id)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            var quiz = await _db.Quizzes
                .Include(q => q.Questions.OrderBy(qq => qq.DisplayOrder))
                    .ThenInclude(qq => qq.Options)
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return NotFound();
            return View(quiz);
        }

        // ── POST /Quiz/AddQuestion ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(int quizId, string questionText,
            int marks, string[] optionTexts, int correctIndex)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Educator", "Admin"))
                return RedirectToAction("Login", "Auth");

            int order = await _db.QuizQuestions
                .Where(q => q.QuizId == quizId)
                .CountAsync() + 1;

            var question = new QuizQuestion
            {
                QuizId       = quizId,
                QuestionText = questionText,
                Marks        = marks,
                DisplayOrder = order
            };
            _db.QuizQuestions.Add(question);
            await _db.SaveChangesAsync();

            for (int i = 0; i < optionTexts.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(optionTexts[i]))
                {
                    _db.QuizOptions.Add(new QuizOption
                    {
                        QuestionId = question.Id,
                        OptionText = optionTexts[i],
                        IsCorrect  = (i == correctIndex)
                    });
                }
            }
            await _db.SaveChangesAsync();

            return RedirectToAction("EditQuestions", new { id = quizId });
        }

        // ── GET /Quiz/Take/5 — Student takes the quiz ────────────────
        public async Task<IActionResult> Take(int id)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Student"))
                return RedirectToAction("Login", "Auth");

            var quiz = await _db.Quizzes
                .Include(q => q.Questions.OrderBy(qq => qq.DisplayOrder))
                    .ThenInclude(qq => qq.Options)
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return NotFound();

            // Check if already attempted
            int userId = SessionHelper.GetUserId(HttpContext);
            ViewBag.Attempted = await _db.QuizAttempts
                .AnyAsync(a => a.QuizId == id && a.StudentId == userId);

            return View(quiz);
        }

        // ── POST /Quiz/Submit — Auto-score and save attempt ──────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuiz(int quizId, IFormCollection form)
        {
            if (SessionHelper.RequireRole(HttpContext, TempData, "Student"))
                return RedirectToAction("Login", "Auth");

            int userId = SessionHelper.GetUserId(HttpContext);

            var quiz = await _db.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(qq => qq.Options)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null) return NotFound();

            int totalQuestions = quiz.Questions.Count;
            int correctAnswers = 0;

            foreach (var question in quiz.Questions)
            {
                string key = $"q_{question.Id}";
                if (form.ContainsKey(key))
                {
                    int selectedOptionId = int.Parse(form[key]!);
                    var selectedOption = question.Options
                        .FirstOrDefault(o => o.Id == selectedOptionId);
                    if (selectedOption?.IsCorrect == true)
                        correctAnswers++;
                }
            }

            int scorePercent = totalQuestions > 0
                ? (int)Math.Round((double)correctAnswers / totalQuestions * 100)
                : 0;

            _db.QuizAttempts.Add(new QuizAttempt
            {
                QuizId         = quizId,
                StudentId      = userId,
                CorrectAnswers = correctAnswers,
                TotalQuestions = totalQuestions,
                ScorePercent   = scorePercent,
                Passed         = scorePercent >= quiz.PassMarkPercent
            });
            await _db.SaveChangesAsync();

            return RedirectToAction("Result", new { quizId, studentId = userId });
        }

        // ── GET /Quiz/Result — Show score ────────────────────────────
        public async Task<IActionResult> Result(int quizId, int studentId)
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            var attempt = await _db.QuizAttempts
                .Include(a => a.Quiz)
                .OrderByDescending(a => a.AttemptedAt)
                .FirstOrDefaultAsync(a => a.QuizId == quizId && a.StudentId == studentId);

            if (attempt == null) return NotFound();
            return View(attempt);
        }
    }
}
```

### B2 — Quiz Views

**`Views/Quiz/Take.cshtml`**
```html
@model PathForge.Models.Entities.Quiz
@{
    ViewData["Title"] = "Quiz — " + Model.Title;
    Layout = "_Layout";
    bool attempted = ViewBag.Attempted == true;
}

<div class="pf-page" style="padding-top:3rem; padding-bottom:3rem;">
  <span class="pf-label">@Model.Course.Title</span>
  <h1 class="pf-section-header" style="margin-top:0.4rem; margin-bottom:0.5rem;">
    @Model.Title
  </h1>
  <p class="pf-body" style="opacity:0.65; margin-bottom:2rem;">
    @Model.Questions.Count questions &nbsp;·&nbsp;
    @Model.TimeLimitMinutes min time limit &nbsp;·&nbsp;
    Pass mark: @Model.PassMarkPercent%
  </p>

  @if (attempted)
  {
    <div class="pf-alert pf-alert--success">
      You have already completed this quiz.
      <a asp-action="Result" asp-route-quizId="@Model.Id"
         asp-route-studentId="@HttpContext.Session.GetString("UserId")"
         style="border-bottom:1px solid var(--dark);">View Result</a>
    </div>
  }
  else
  {
    <form asp-action="SubmitQuiz" method="post">
      @Html.AntiForgeryToken()
      <input type="hidden" name="quizId" value="@Model.Id" />

      @{int qNum = 0;}
      @foreach (var question in Model.Questions)
      {
        qNum++;
        <div class="pf-card pf-card--outlined" style="margin-bottom:1.25rem; padding:1.25rem 1.5rem 1.25rem 1.25rem;">
          <p class="pf-body" style="font-weight:700; margin-bottom:0.75rem;">
            @qNum. @question.QuestionText
            <span class="pf-label" style="display:inline; margin-left:0.5rem; font-size:0.55rem;">
              @question.Marks mark@(question.Marks != 1 ? "s" : "")
            </span>
          </p>

          @foreach (var option in question.Options)
          {
            <label style="display:flex; align-items:center; gap:0.6rem; margin-bottom:0.4rem;
                          cursor:pointer; font-family:var(--font-chrome); font-size:0.8rem;">
              <input type="radio" name="q_@question.Id" value="@option.Id" required
                     style="accent-color:var(--red);" />
              @option.OptionText
            </label>
          }
        </div>
      }

      <button type="submit" class="pf-btn" style="margin-top:0.5rem;">
        Submit Quiz
      </button>
    </form>
  }
</div>
```

**`Views/Quiz/Result.cshtml`**
```html
@model PathForge.Models.Entities.QuizAttempt
@{
    ViewData["Title"] = "Quiz Result";
    Layout = "_Layout";
}

<div class="pf-page" style="padding-top:5rem; padding-bottom:5rem; text-align:center;">

  <span class="pf-label" style="display:block;">Quiz Result</span>
  <h1 class="pf-section-header" style="margin-top:0.5rem; margin-bottom:0.5rem;">
    @Model.Quiz.Title
  </h1>

  {{!-- Big stat number — FRAME.md stat-big --}}
  <div style="margin:2.5rem 0;">
    <div class="pf-stat-big" style="display:block; text-align:center;">
      @Model.ScorePercent%
    </div>
    <span class="pf-label" style="display:block; margin-top:1rem; font-size:0.75rem;">
      @Model.CorrectAnswers of @Model.TotalQuestions correct
    </span>
  </div>

  {{!-- Pass/Fail block --}}
  @if (Model.Passed)
  {
    <div class="pf-alert pf-alert--success" style="display:inline-block; padding:0.75rem 2rem;">
      PASSED — Well done!
    </div>
  }
  else
  {
    <div class="pf-alert pf-alert--error" style="display:inline-block; padding:0.75rem 2rem;">
      NOT PASSED — Required @Model.Quiz.PassMarkPercent%
    </div>
  }

  {{!-- Progress bar for score --}}
  <div class="pf-progress" style="margin:2rem auto; max-width:400px;">
    <div class="pf-progress__fill" style="width:@Model.ScorePercent%;"></div>
  </div>

  <div style="display:flex; gap:0.75rem; justify-content:center; margin-top:1.5rem;">
    <a asp-controller="Course" asp-action="Details"
       asp-route-id="@Model.Quiz.CourseId" class="pf-btn">
      Back to Course
    </a>
  </div>

</div>
```

---

## PART C — DISCUSSION FORUM

### C1 — Discussion Controller

**`Controllers/DiscussionController.cs`**
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PathForge.Data;
using PathForge.Helpers;
using PathForge.Models.Entities;

namespace PathForge.Controllers
{
    public class DiscussionController : Controller
    {
        private readonly PathForgeDbContext _db;

        public DiscussionController(PathForgeDbContext db) { _db = db; }

        // ── GET /Discussion/Index/courseId — Thread list ─────────────
        public async Task<IActionResult> Index(int courseId)
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            var course = await _db.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var threads = await _db.DiscussionPosts
                .Include(p => p.Author)
                .Include(p => p.Replies)
                .Where(p => p.CourseId == courseId && p.ParentPostId == null)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Course   = course;
            ViewBag.UserId   = SessionHelper.GetUserId(HttpContext);
            ViewBag.UserRole = SessionHelper.GetUserRole(HttpContext);

            return View(threads);
        }

        // ── POST /Discussion/CreateThread ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateThread(int courseId, string title, string body)
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            _db.DiscussionPosts.Add(new DiscussionPost
            {
                CourseId     = courseId,
                AuthorId     = SessionHelper.GetUserId(HttpContext),
                Title        = title,
                Body         = body,
                ParentPostId = null
            });
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", new { courseId });
        }

        // ── GET /Discussion/Thread/5 — View thread + replies ─────────
        public async Task<IActionResult> Thread(int id)
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            var thread = await _db.DiscussionPosts
                .Include(p => p.Author)
                .Include(p => p.Course)
                .Include(p => p.Replies.OrderBy(r => r.CreatedAt))
                    .ThenInclude(r => r.Author)
                .FirstOrDefaultAsync(p => p.Id == id && p.ParentPostId == null);

            if (thread == null) return NotFound();

            ViewBag.UserId   = SessionHelper.GetUserId(HttpContext);
            ViewBag.UserRole = SessionHelper.GetUserRole(HttpContext);

            return View(thread);
        }

        // ── POST /Discussion/Reply ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int threadId, int courseId, string body)
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            _db.DiscussionPosts.Add(new DiscussionPost
            {
                CourseId     = courseId,
                AuthorId     = SessionHelper.GetUserId(HttpContext),
                Title        = "Re: thread",
                Body         = body,
                ParentPostId = threadId
            });
            await _db.SaveChangesAsync();

            return RedirectToAction("Thread", new { id = threadId });
        }

        // ── POST /Discussion/Delete/5 — Admin or author can delete ───
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            var post = await _db.DiscussionPosts.FindAsync(id);
            if (post == null) return NotFound();

            int userId = SessionHelper.GetUserId(HttpContext);
            string role = SessionHelper.GetUserRole(HttpContext);

            if (post.AuthorId != userId && role != "Admin")
                return Forbid();

            int courseId = post.CourseId;
            _db.DiscussionPosts.Remove(post);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", new { courseId });
        }
    }
}
```

### C2 — Discussion Views

**`Views/Discussion/Index.cshtml`**
```html
@model IEnumerable<PathForge.Models.Entities.DiscussionPost>
@{
    ViewData["Title"] = "Discussion — " + (ViewBag.Course as PathForge.Models.Entities.Course)?.Title;
    Layout = "_Layout";
    var course = ViewBag.Course as PathForge.Models.Entities.Course;
}

<div class="pf-page" style="padding-top:3rem; padding-bottom:3rem;">

  <div style="border-bottom:var(--border-outer); padding-bottom:1.5rem; margin-bottom:2rem;">
    <span class="pf-label">@course?.Title</span>
    <h1 class="pf-section-header" style="margin-top:0.4rem;">Discussion Forum</h1>
  </div>

  {{!-- New thread form --}}
  <div class="pf-card pf-card--outlined" style="margin-bottom:2rem; padding:1.25rem;">
    <h2 class="pf-card-title" style="font-size:1.1rem; margin-bottom:1rem;">
      Start a Discussion
    </h2>
    <form asp-action="CreateThread" method="post">
      @Html.AntiForgeryToken()
      <input type="hidden" name="courseId" value="@course?.Id" />
      <div class="pf-form-group">
        <label class="pf-form-label">Subject</label>
        <input type="text" name="title" class="pf-input" required
               placeholder="What do you want to discuss?" />
      </div>
      <div class="pf-form-group">
        <label class="pf-form-label">Message</label>
        <textarea name="body" class="pf-input pf-textarea" required
                  placeholder="Write your question or comment..."></textarea>
      </div>
      <button type="submit" class="pf-btn">Post Thread</button>
    </form>
  </div>

  {{!-- Thread list --}}
  @if (!Model.Any())
  {
    <div class="pf-alert">No discussions yet. Start the first one above.</div>
  }
  else
  {
    @foreach (var thread in Model)
    {
      <div class="pf-card pf-card--outlined" style="margin-bottom:0.75rem; padding:1rem 1.25rem;">
        <div style="display:flex; justify-content:space-between; align-items:start;">
          <div>
            <a asp-action="Thread" asp-route-id="@thread.Id"
               class="pf-card-title" style="font-size:1.05rem; color:var(--dark);
               text-decoration:none; border-bottom:1px solid var(--red);">
              @thread.Title
            </a>
            <p class="pf-body-cell" style="margin-top:0.25rem; opacity:0.6;">
              By @thread.Author.FullName &nbsp;·&nbsp;
              @thread.CreatedAt.ToString("dd MMM yyyy") &nbsp;·&nbsp;
              @thread.Replies.Count @(thread.Replies.Count == 1 ? "reply" : "replies")
            </p>
          </div>
        </div>
      </div>
    }
  }
</div>
```

**`Views/Discussion/Thread.cshtml`**
```html
@model PathForge.Models.Entities.DiscussionPost
@{
    ViewData["Title"] = Model.Title;
    Layout = "_Layout";
    int userId   = (int)(ViewBag.UserId ?? 0);
    string role  = ViewBag.UserRole ?? "";
}

<div class="pf-page" style="padding-top:3rem; padding-bottom:3rem;">

  <a asp-action="Index" asp-route-courseId="@Model.CourseId"
     class="pf-label" style="text-decoration:none; margin-bottom:1.5rem; display:inline-block;">
    ← Back to Forum
  </a>

  {{!-- Original post --}}
  <div class="pf-card pf-card--outlined" style="padding:1.5rem; margin-top:1rem; margin-bottom:2rem;">
    <h1 class="pf-section-header" style="font-size:1.6rem;">@Model.Title</h1>
    <p class="pf-body-cell" style="margin:0.3rem 0 1rem; opacity:0.6;">
      By @Model.Author.FullName &nbsp;·&nbsp; @Model.CreatedAt.ToString("dd MMM yyyy, HH:mm")
    </p>
    <p class="pf-body">@Model.Body</p>
    @if (userId == Model.AuthorId || role == "Admin")
    {
      <form asp-action="Delete" method="post" style="margin-top:1rem;">
        @Html.AntiForgeryToken()
        <input type="hidden" name="id" value="@Model.Id" />
        <button type="submit" class="pf-btn pf-btn--danger pf-btn--sm"
                onclick="return confirm('Delete this post?')">Delete</button>
      </form>
    }
  </div>

  {{!-- Replies --}}
  <h2 class="pf-card-title" style="margin-bottom:1rem;">
    Replies (@Model.Replies.Count)
  </h2>

  @foreach (var reply in Model.Replies)
  {
    <div class="pf-card" style="margin-bottom:0.75rem; padding:1rem 1rem 1rem 1.25rem;
         border-left:var(--border-leftbar); background:var(--light);">
      <p class="pf-body-cell" style="opacity:0.6; margin-bottom:0.4rem;">
        @reply.Author.FullName &nbsp;·&nbsp; @reply.CreatedAt.ToString("dd MMM, HH:mm")
      </p>
      <p class="pf-body" style="font-size:0.9rem;">@reply.Body</p>
    </div>
  }

  {{!-- Reply form --}}
  <div style="margin-top:2rem; border-top:var(--border-outer); padding-top:1.5rem;">
    <h3 class="pf-card-title" style="font-size:1rem; margin-bottom:1rem;">Leave a Reply</h3>
    <form asp-action="Reply" method="post">
      @Html.AntiForgeryToken()
      <input type="hidden" name="threadId" value="@Model.Id" />
      <input type="hidden" name="courseId" value="@Model.CourseId" />
      <div class="pf-form-group">
        <label class="pf-form-label">Your Reply</label>
        <textarea name="body" class="pf-input pf-textarea" required
                  placeholder="Write your reply..."></textarea>
      </div>
      <button type="submit" class="pf-btn">Post Reply</button>
    </form>
  </div>
</div>
```

---

## PART D — LEARNING PATH CONSTELLATION + PROGRESS

### D1 — Progress Controller

**`Controllers/ProgressController.cs`**
```csharp
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
```

### D2 — Progress / Constellation View

**`Views/Progress/Index.cshtml`**
```html
@model IEnumerable<PathForge.Models.Entities.CourseEnrolment>
@{
    ViewData["Title"] = "My Progress";
    Layout = "_Layout";
    var progressRecords = ViewBag.ProgressRecords as
        List<PathForge.Models.Entities.StudentProgress>
        ?? new List<PathForge.Models.Entities.StudentProgress>();
}

<div class="pf-page" style="padding-top:3rem; padding-bottom:3rem;">

  <span class="pf-label">Student Dashboard</span>
  <h1 class="pf-section-header" style="margin-top:0.4rem; margin-bottom:2.5rem;">
    Learning Path Constellation
  </h1>

  @if (!Model.Any())
  {
    <div class="pf-alert">
      You are not enrolled in any courses yet.
      <a asp-controller="Course" asp-action="Index" style="border-bottom:1px solid var(--dark);">
        Browse courses
      </a>
    </div>
  }

  @foreach (var enrolment in Model)
  {
    var course  = enrolment.Course;
    var modules = course.Modules.ToList();
    int total   = modules.Count;
    int done    = progressRecords.Count(p =>
        modules.Any(m => m.Id == p.ModuleId) && p.Status == "Completed");
    int pct     = total > 0 ? (int)Math.Round((double)done / total * 100) : 0;

    <div class="pf-card pf-card--outlined" style="margin-bottom:2.5rem; padding:1.5rem;">

      {{!-- Course header --}}
      <div style="display:flex; justify-content:space-between; align-items:start;
                  margin-bottom:1.25rem; flex-wrap:wrap; gap:1rem;">
        <div>
          <span class="pf-label">@course.Category</span>
          <h2 class="pf-card-title" style="margin-top:0.25rem;">@course.Title</h2>
          <p class="pf-body-cell" style="margin-top:0.2rem; opacity:0.65;">
            @done of @total modules complete
          </p>
        </div>
        <div>
          <span class="pf-stat-cell__number" style="font-size:2rem;">@pct%</span>
          <span class="pf-stat-cell__label">Complete</span>
        </div>
      </div>

      {{!-- Progress bar --}}
      <div class="pf-progress" style="margin-bottom:1.5rem;">
        <div class="pf-progress__fill" style="width:@pct%;"></div>
      </div>

      {{!-- CONSTELLATION — the star map ──────────────────────── --}}
      <div class="constellation-wrap" id="constellation-@course.Id"
           style="position:relative; overflow-x:auto;">
        <svg class="constellation-svg"
             viewBox="0 0 @(Math.Max(modules.Count * 120, 400)) 160"
             xmlns="http://www.w3.org/2000/svg"
             style="width:100%; min-width:@(modules.Count * 100)px; height:160px;">

          {{!-- Draw connecting lines first (behind nodes) --}}
          @for (int i = 0; i < modules.Count - 1; i++)
          {
            var x1 = 60 + i * 120;
            var x2 = 60 + (i + 1) * 120;
            var y  = 80;

            var thisStatus = progressRecords
                .FirstOrDefault(p => p.ModuleId == modules[i].Id)?.Status ?? "Locked";
            var nextStatus = progressRecords
                .FirstOrDefault(p => p.ModuleId == modules[i + 1].Id)?.Status ?? "Locked";

            var lineColor = (thisStatus == "Completed" && nextStatus != "Locked")
                ? "#D8000F" : "#C0B8B0";
            var lineDash  = (nextStatus == "Locked") ? "4 4" : "none";

            <line x1="@x1" y1="@y" x2="@x2" y2="@y"
                  stroke="@lineColor" stroke-width="2"
                  stroke-dasharray="@lineDash" />
          }

          {{!-- Draw nodes (stars) on top of lines --}}
          @for (int i = 0; i < modules.Count; i++)
          {
            var module = modules[i];
            var cx     = 60 + i * 120;
            var cy     = 80;

            var prog   = progressRecords.FirstOrDefault(p => p.ModuleId == module.Id);
            var status = prog?.Status ?? "Locked";

            string fillColor   = status == "Completed"  ? "#D8000F"
                               : status == "InProgress" ? "#1C1410"
                               :                          "#C0B8B0";
            string strokeColor = status == "Locked" ? "#C0B8B0" : "#D8000F";
            int    radius      = status == "InProgress" ? 20 : 14;
            string pulseClass  = status == "InProgress" ? "star-pulse" : "";

            <g class="constellation-node @pulseClass"
               data-status="@status"
               data-module-id="@module.Id">

              {{!-- Outer ring for InProgress --}}
              @if (status == "InProgress")
              {
                <circle cx="@cx" cy="@cy" r="26" fill="none"
                        stroke="#D8000F" stroke-width="1.5"
                        opacity="0.4" class="star-ring" />
              }

              {{!-- The node --}}
              <circle cx="@cx" cy="@cy" r="@radius"
                      fill="@fillColor" stroke="@strokeColor" stroke-width="2" />

              {{!-- Star icon inside completed nodes --}}
              @if (status == "Completed")
              {
                <text x="@cx" y="@(cy + 5)" text-anchor="middle"
                      font-size="13" fill="#FFFFFF">★</text>
              }
              @if (status == "InProgress")
              {
                <text x="@cx" y="@(cy + 5)" text-anchor="middle"
                      font-size="13" fill="#F5F2EF">✦</text>
              }

              {{!-- Module label below --}}
              <text x="@cx" y="@(cy + radius + 16)"
                    text-anchor="middle" font-size="10"
                    font-family="'Space Grotesk', sans-serif"
                    fill="@(status == "Locked" ? "#C0B8B0" : "#1C1410")"
                    font-weight="600">
                @(module.Title.Length > 14 ? module.Title.Substring(0, 13) + "…" : module.Title)
              </text>

              {{!-- Order number inside locked nodes --}}
              @if (status == "Locked")
              {
                <text x="@cx" y="@(cy + 5)" text-anchor="middle"
                      font-size="11" fill="#FFFFFF" font-weight="600">
                  @module.DisplayOrder
                </text>
              }
            </g>
          }
        </svg>
      </div>

      {{!-- Mark complete button for InProgress module --}}
      @{
        var inProg = modules.FirstOrDefault(m =>
          progressRecords.Any(p => p.ModuleId == m.Id && p.Status == "InProgress"));
      }
      @if (inProg != null)
      {
        <form asp-action="MarkComplete" method="post" style="margin-top:1rem;">
          @Html.AntiForgeryToken()
          <input type="hidden" name="moduleId" value="@inProg.Id" />
          <button type="submit" class="pf-btn pf-btn--sm">
            Mark "@inProg.Title" Complete
          </button>
        </form>
      }

    </div>
  }
</div>

@section Styles {
<style>
/* Pulse animation for InProgress node */
.star-ring {
  animation: starPulse 2s ease-in-out infinite;
  transform-box: fill-box;
  transform-origin: center;
}

@keyframes starPulse {
  0%, 100% { opacity: 0.2; r: 26px; }
  50%       { opacity: 0.7; r: 30px; }
}
</style>
}
```

---

## PART E — ADMIN PANEL

### E1 — Admin Controller

**`Controllers/AdminController.cs`**
```csharp
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

            ViewBag.TotalUsers     = await _db.Users.CountAsync();
            ViewBag.ActiveCourses  = await _db.Courses.CountAsync(c => c.Status == "Live");
            ViewBag.TotalEnrolments= await _db.CourseEnrolments.CountAsync();
            ViewBag.FlaggedPosts   = await _db.DiscussionPosts.CountAsync(p => p.IsFlagged);

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
```

### E2 — Admin Dashboard View

**`Views/Admin/Index.cshtml`**
```html
@model IEnumerable<PathForge.Models.Entities.User>
@{
    ViewData["Title"] = "Admin Console";
    Layout = "_Layout";
}

<div class="pf-page" style="padding-top:3rem; padding-bottom:3rem;">

  <span class="pf-label">Root Admin</span>
  <h1 class="pf-section-header" style="margin-top:0.4rem; margin-bottom:2rem;">
    Admin Console
  </h1>

  {{!-- Stats grid (fin-grid) --}}
  <div class="pf-stat-grid" style="margin-bottom:3rem;">
    <div class="pf-stat-cell">
      <span class="pf-stat-cell__number">@ViewBag.TotalUsers</span>
      <span class="pf-stat-cell__label">Total Users</span>
    </div>
    <div class="pf-stat-cell">
      <span class="pf-stat-cell__number">@ViewBag.ActiveCourses</span>
      <span class="pf-stat-cell__label">Live Courses</span>
    </div>
    <div class="pf-stat-cell">
      <span class="pf-stat-cell__number">@ViewBag.TotalEnrolments</span>
      <span class="pf-stat-cell__label">Enrolments</span>
    </div>
    <div class="pf-stat-cell">
      <span class="pf-stat-cell__number">@ViewBag.FlaggedPosts</span>
      <span class="pf-stat-cell__label">Flagged Posts</span>
    </div>
  </div>

  {{!-- Recent users --}}
  <h2 class="pf-card-title" style="margin-bottom:1rem;">Recent Users</h2>
  <div class="pf-table-wrap">
    <table class="pf-table">
      <thead>
        <tr>
          <th>Name</th>
          <th>Email</th>
          <th>Role</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        @foreach (var user in Model)
        {
          <tr>
            <td>@user.FullName</td>
            <td>@user.Email</td>
            <td><span class="pf-label" style="font-size:0.55rem;">@user.Role</span></td>
            <td>
              <span class="pf-status @(user.IsActive ? "pf-status--live" : "pf-status--draft")">
                @(user.IsActive ? "Active" : "Suspended")
              </span>
            </td>
            <td>
              <form asp-action="SuspendUser" asp-route-id="@user.Id" method="post">
                @Html.AntiForgeryToken()
                <button type="submit" class="pf-btn pf-btn--sm pf-btn--ghost">
                  @(user.IsActive ? "Suspend" : "Reactivate")
                </button>
              </form>
            </td>
          </tr>
        }
      </tbody>
    </table>
  </div>

  <div style="display:flex; gap:0.75rem; margin-top:1.5rem;">
    <a asp-action="Users"   class="pf-btn pf-btn--ghost pf-btn--sm">All Users</a>
    <a asp-action="Courses" class="pf-btn pf-btn--ghost pf-btn--sm">All Courses</a>
  </div>

</div>
```

---

## WEEK 4 DONE — CHECKLIST

**Assignments:**
- [ ] Educator can create assignments with due date and max marks
- [ ] Student can submit a file and comment
- [ ] Late submission gets "Late" status automatically
- [ ] Educator can grade and leave feedback
- [ ] Student sees their grade and feedback

**Quizzes:**
- [ ] Educator can create quiz with multiple questions and options
- [ ] Each question has exactly one correct option
- [ ] Auto-scoring works — correct answers calculated server-side
- [ ] Result page shows score, pass/fail, and progress bar

**Discussions:**
- [ ] Students and educators can post threads
- [ ] Anyone can reply
- [ ] Author and Admin can delete posts

**Progress / Constellation:**
- [ ] Constellation SVG renders nodes for each module
- [ ] Completed = red filled star, InProgress = dark pulsing, Locked = grey
- [ ] Clicking "Mark Complete" unlocks the next module
- [ ] Progress percentage bar updates correctly

**Admin:**
- [ ] Dashboard shows 4 stat cells
- [ ] Admin can suspend/reactivate users
- [ ] Non-admin trying to access `/Admin` is blocked

---

## WHAT PHASE 5 COVERS

Phase 5 is the final checklist, testing guide, and requirements verification.
