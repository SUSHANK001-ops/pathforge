# PHASE 5 — FINAL CHECKLIST, TESTING & REQUIREMENTS AUDIT
### PathForge · Week 5 of 5

> This file is your pre-submission gate.
> Do not submit until every checkbox is ticked.
> Work through each section in one sitting per section.

---

## SECTION 1 — ASSIGNMENT BRIEF REQUIREMENTS

These come directly from the CT050-3-2-WAPP assignment document.
Every item is mandatory.

### 1A — Technical Requirements

| # | Requirement | Where to check | Status |
|---|---|---|---|
| 1 | Interlinked web pages | Click every link in the navbar and every "Back" button | ☐ |
| 2 | HTML5 elements used properly | Check `<nav>`, `<main>`, `<section>`, `<article>`, `<form>`, `<label>`, semantic headings in every view | ☐ |
| 3 | External CSS file used | `site.css` exists in `wwwroot/css/` and is linked in `_Layout.cshtml` | ☐ |
| 4 | Internal CSS used | CSS inside a `<style>` tag in at least one view (e.g. Progress Index) | ☐ |
| 5 | Inline style used | `style="..."` attribute present on at least one element | ☐ |
| 6 | Database: INSERT records | Register → creates User row; Create Course → inserts Course row | ☐ |
| 7 | Database: SELECT / Display | Course catalogue fetches from DB; Admin users table from DB | ☐ |
| 8 | Database: UPDATE records | Edit Course, Edit Profile, Mark Assignment as Graded | ☐ |
| 9 | Database: DELETE records | Delete Course, Delete Module, Suspend User (soft), Delete Discussion Post | ☐ |
| 10 | Registration page | `/Auth/Register` — FullName, Email, Role, Password, ConfirmPassword | ☐ |
| 11 | Registered member module (login required) | Student dashboard, Progress page, Assignment submit — all redirect to login if not logged in | ☐ |
| 12 | Administrator module (login required) | `/Admin` — blocked for non-Admin roles | ☐ |
| 13 | Form validation | Both client-side (JS in site.js) and server-side (ModelState + DataAnnotations) | ☐ |
| 14 | Navigation support | Navbar on every page, back links in views, breadcrumb-style labels | ☐ |
| 15 | File organization | Matches PHASE 1 folder structure exactly, correct naming convention | ☐ |
| 16 | Database connectivity | EF Core connected to SQL Server; `dotnet ef database update` succeeded | ☐ |
| 17 | Client-side scripting | `site.js` — form validation, progress strip | ☐ |
| 18 | Server-side scripting | All controllers contain logic that runs on the server | ☐ |
| 19 | Multimedia elements | Course thumbnail images, embedded file links (PDF, Video URLs) in materials | ☐ |
| 20 | Web standards | Validate at least one page at https://validator.w3.org | ☐ |

### 1B — Whiteboard Features (from the classroom image)

Every feature shown on the board must be present in the final submission.

| Feature on Board | Where in PathForge | Status |
|---|---|---|
| User Registration / Management | `/Auth/Register`, `/Auth/Profile`, `/Admin/Users` | ☐ |
| Course Management | `/Course/Manage`, `/Course/Create`, `/Course/Edit` | ☐ |
| Learning Materials | `/Course/EditModules` → UploadMaterial | ☐ |
| Assignment / Quiz | `/Assignment/`, `/Quiz/` controllers and views | ☐ |
| Student Progress (Learning/Result) | `/Progress/Index` — Constellation + percentage | ☐ |
| Communication / Chats | `/Discussion/` — forum threads and replies | ☐ |
| Profile Management | `/Auth/Profile` — edit name, bio, password | ☐ |
| Lessons + Videos | Module types: Lesson, Video — shown in Course Details | ☐ |
| Virtual Lab (creative feature) | Learning Path Constellation is the creative equivalent | ☐ |

---

## SECTION 2 — DESIGN SYSTEM AUDIT (FRAME.md Compliance)

Check every page against the design system before submitting.

### 2A — Color Rule
**Four colors only: `#FFFFFF`, `#1C1410`, `#D8000F`, `#F5F2EF`**
No blue, green, yellow, orange, or purple should appear anywhere.

- [ ] Open every page and look for any color outside the four
- [ ] Check button hover states
- [ ] Check error states
- [ ] Check form focus rings (must be red)
- [ ] No gradient anywhere

### 2B — Typography Rule
**Three fonts only:**
- Shrikhand — display, section headers, card titles, stat numbers
- Libre Baskerville — all body paragraphs, descriptions, cell text
- Space Grotesk — labels, eyebrows, button text, navigation links

**Check:**
- [ ] Every `h1` uses `.pf-section-header` (Shrikhand) or `.pf-hero-title`
- [ ] Every `h2` / `h3` in cards uses `.pf-card-title` (Shrikhand)
- [ ] Every body paragraph uses `.pf-body` or `.pf-body-cell` (Baskerville)
- [ ] Every label / eyebrow above a heading uses `.pf-label` (Space Grotesk, uppercase, red)
- [ ] Navigation links use Space Grotesk uppercase tracked
- [ ] Button text uses Space Grotesk uppercase tracked
- [ ] No system font (Arial, Helvetica, sans-serif) appears on any user-visible text

### 2C — Shape Rule
**Square corners everywhere except the hint pill (4px).**

- [ ] All buttons: `border-radius: 0`
- [ ] All input fields: `border-radius: 0`
- [ ] All cards: `border-radius: 0`
- [ ] All panels: `border-radius: 0`
- [ ] The ONLY 4px radius elements are `.pf-status` pills

### 2D — Shadow Rule
**No `box-shadow` anywhere. Only `text-shadow` on red display text.**

Open browser DevTools → Elements → Computed → search "box-shadow".
It must say `none` on every element except browser defaults.

- [ ] No card shadows
- [ ] No button shadows
- [ ] No modal/overlay shadows
- [ ] Stacked text-shadow CSS variable only used on `.pf-stat-big` if displayed on red background

### 2E — Border Rule (FRAME.md signature)
- [ ] Data tables use 3px outer border + 1.5px inner cell borders
- [ ] All cards use 4px red left border
- [ ] Form panels use 3px dark outer border
- [ ] Navbar bottom border is 3px red

### 2F — Component Checklist
| Component | Present | Styled correctly |
|---|---|---|
| `.pf-navbar` | ☐ | ☐ |
| `.pf-btn` (red, square, Space Grotesk) | ☐ | ☐ |
| `.pf-btn--ghost` | ☐ | ☐ |
| `.pf-input` (square, 2px dark border, red focus) | ☐ | ☐ |
| `.pf-form-label` (Space Grotesk, uppercase, red) | ☐ | ☐ |
| `.pf-card` (red leftbar, no shadow) | ☐ | ☐ |
| `.pf-table-wrap` + `.pf-table` (double border) | ☐ | ☐ |
| `.pf-progress` + `.pf-progress__fill` (red) | ☐ | ☐ |
| `.pf-stat-grid` + `.pf-stat-cell` | ☐ | ☐ |
| `.pf-alert` (red leftbar) | ☐ | ☐ |
| `.pf-status` pill (4px radius — the exception) | ☐ | ☐ |
| Constellation SVG (red = complete, dark = in-progress, grey = locked) | ☐ | ☐ |

---

## SECTION 3 — FUNCTIONAL TESTING GUIDE

Run each test case manually. Repeat each as all three roles.

### 3A — Auth Tests

| Test | Expected Result | Pass? |
|---|---|---|
| Register with valid data | Account created, redirected to login, success message shows | ☐ |
| Register with duplicate email | Error: "An account with this email already exists" | ☐ |
| Register with password < 8 chars | Validation fires before submit, form not sent | ☐ |
| Register with mismatched passwords | Validation error: "Passwords do not match" | ☐ |
| Login with correct credentials | Session set, redirected to home | ☐ |
| Login with wrong password | Error: "Incorrect email or password" | ☐ |
| Login with suspended account | Error (user not found as `IsActive = false`) | ☐ |
| Logout | Session cleared, redirected to login page | ☐ |
| Access `/Admin` as Student | Blocked, error message shown | ☐ |
| Access `/Admin` as not logged in | Redirected to login | ☐ |
| Profile: update name | Name changes in navbar and DB | ☐ |
| Profile: change password | Old password no longer works, new one does | ☐ |

### 3B — Course Tests

| Test | Expected Result | Pass? |
|---|---|---|
| Educator creates course (Draft) | Appears in Manage with Draft status | ☐ |
| Educator publishes course | Status changes to Live, visible in catalogue | ☐ |
| Educator unpublishes course | Status changes to Draft, hidden from catalogue | ☐ |
| Educator deletes course | Row removed from DB, gone from Manage list | ☐ |
| Educator adds module | Module appears in EditModules list | ☐ |
| Educator uploads Document file | File saved to `/uploads/materials/`, link appears | ☐ |
| Educator adds Link type | No file upload — URL saved directly | ☐ |
| Student browses catalogue | Only Live courses shown | ☐ |
| Student searches by title | Matching courses filtered | ☐ |
| Student filters by category | Only that category shown | ☐ |
| Student enrols | Enrolment row created, progress rows created | ☐ |
| Student tries to enrol twice | No duplicate — button replaced by "My Progress" | ☐ |
| Non-enrolled student views Details | Materials locked, "Enrol to access" shown | ☐ |

### 3C — Assignment Tests

| Test | Expected Result | Pass? |
|---|---|---|
| Educator creates assignment | Appears in course assignment list | ☐ |
| Student submits before due date | Status = "Submitted" | ☐ |
| Student submits after due date | Status = "Late" | ☐ |
| Student submits twice | Error: "already submitted" | ☐ |
| Educator grades submission | Marks and feedback saved; status = "Graded" | ☐ |
| Student views graded submission | Sees marks and feedback | ☐ |

### 3D — Quiz Tests

| Test | Expected Result | Pass? |
|---|---|---|
| Educator creates quiz with 3 questions | All 3 appear in EditQuestions | ☐ |
| Student takes quiz — all correct | Score = 100%, Passed = true | ☐ |
| Student takes quiz — all wrong | Score = 0%, Passed = false (if pass mark > 0) | ☐ |
| Student tries to take completed quiz | "Already completed" message shown | ☐ |
| Result page shows correct score | ScorePercent, CorrectAnswers, TotalQuestions correct | ☐ |

### 3E — Discussion Tests

| Test | Expected Result | Pass? |
|---|---|---|
| User posts a thread | Thread appears in course forum list | ☐ |
| User replies to thread | Reply appears under the thread | ☐ |
| Author deletes own post | Post removed | ☐ |
| Non-author tries to delete | Forbidden (403) | ☐ |
| Admin deletes any post | Post removed regardless of author | ☐ |

### 3F — Progress Tests

| Test | Expected Result | Pass? |
|---|---|---|
| Enrol in course with 4 modules | Module 1 = InProgress, 2-4 = Locked | ☐ |
| Mark Module 1 complete | Module 1 = Completed, Module 2 = InProgress | ☐ |
| Complete all modules | Progress = 100%, all nodes red in constellation | ☐ |
| Constellation renders correctly | Completed = red star, InProgress = dark + pulse, Locked = grey | ☐ |

### 3G — Admin Tests

| Test | Expected Result | Pass? |
|---|---|---|
| Dashboard shows correct stat numbers | Values match actual DB counts | ☐ |
| Suspend a user | IsActive = false in DB, user cannot log in | ☐ |
| Reactivate a user | IsActive = true, user can log in again | ☐ |
| Admin views all courses | Courses from all educators shown | ☐ |

---

## SECTION 4 — SECURITY CHECKLIST

### 4A — Authentication
- [ ] Passwords are stored as BCrypt hashes — never plain text
- [ ] Check the Users table in SQL Server — PasswordHash column contains `$2a$...` format
- [ ] Session expires after 2 hours of inactivity (set in `Program.cs`)
- [ ] Suspended users cannot log in (IsActive check in login)

### 4B — Authorization
- [ ] Every controller action that requires a role calls `SessionHelper.RequireRole`
- [ ] Educator cannot manage another educator's course (CanManageCourse check)
- [ ] Student cannot access educator-only routes
- [ ] Non-admin cannot access admin routes

### 4C — CSRF Protection
- [ ] Every `<form method="post">` has `@Html.AntiForgeryToken()`
- [ ] Every POST action has `[ValidateAntiForgeryToken]` attribute
- [ ] No exceptions — including delete forms, status change forms, grading forms

### 4D — Input Validation
- [ ] All ViewModels have `[Required]`, `[MaxLength]`, `[EmailAddress]` etc. where needed
- [ ] `ModelState.IsValid` checked in every POST action before saving to DB
- [ ] File uploads: only allow expected extensions (optional but professional)

### 4E — SQL Injection
- [ ] All DB queries use EF Core LINQ — no raw SQL string concatenation
- [ ] Double-check: no `_db.Database.ExecuteSqlRaw()` with unparameterized strings

---

## SECTION 5 — ADDITIONAL FEATURES BEYOND THE BRIEF

These add marks in the Distinction band. Implement at least 2.

### Option A — Email notification on submission
When a student submits an assignment, show the educator a visual badge in the Manage view
(count of ungraded submissions).

**How:** In `Views/Course/Manage.cshtml`, add to the Actions column:
```html
@{
  int ungraded = course.Assignments
    .SelectMany(a => a.Submissions)
    .Count(s => s.Status == "Submitted" || s.Status == "Late");
}
@if (ungraded > 0)
{
  <span class="pf-status pf-status--live" style="margin-right:0.25rem;">
    @ungraded new
  </span>
}
```
Make sure to include `.ThenInclude(a => a.Submissions)` in the Manage query.

---

### Option B — Quiz timer (countdown in the browser)
In `Views/Quiz/Take.cshtml`, add a countdown before the submit button:

```html
<div id="quiz-timer" style="font-family:var(--font-display); font-size:2rem;
     color:var(--red); margin-bottom:1rem;">
  @Model.TimeLimitMinutes:00
</div>

@section Scripts {
<script>
  var minutes = @Model.TimeLimitMinutes;
  var seconds = 0;
  var display = document.getElementById('quiz-timer');
  var form    = document.querySelector('form');

  var interval = setInterval(function () {
    if (seconds === 0) {
      if (minutes === 0) {
        clearInterval(interval);
        form.submit();   // auto-submit when time runs out
        return;
      }
      minutes--;
      seconds = 59;
    } else {
      seconds--;
    }
    display.textContent =
      String(minutes).padStart(2,'0') + ':' + String(seconds).padStart(2,'0');

    // Turn red in last minute
    if (minutes === 0) display.style.color = 'var(--red)';
  }, 1000);
</script>
}
```

---

### Option C — Search across all content (global search)
Add a search route to `HomeController`:

```csharp
public async Task<IActionResult> Search(string q)
{
    if (string.IsNullOrWhiteSpace(q)) return RedirectToAction("Index");

    var courses = await _db.Courses
        .Where(c => c.Status == "Live" &&
                    (c.Title.Contains(q) || c.Description.Contains(q)))
        .Take(10)
        .ToListAsync();

    var discussions = await _db.DiscussionPosts
        .Include(p => p.Course)
        .Where(p => p.Title.Contains(q) || p.Body.Contains(q))
        .Take(10)
        .ToListAsync();

    ViewBag.Query       = q;
    ViewBag.Courses     = courses;
    ViewBag.Discussions = discussions;

    return View();
}
```

Add a search input to the navbar and link it to `/Home/Search?q=...`.

---

### Option D — Course progress percentage in the catalogue card
In `Views/Course/Index.cshtml`, for logged-in enrolled students, show
their percentage progress on each course card:

```csharp
// In the controller Index action, after the query
int userId = SessionHelper.GetUserId(HttpContext);
if (userId > 0)
{
    var progressData = await _db.StudentProgress
        .Where(p => p.StudentId == userId)
        .ToListAsync();
    ViewBag.ProgressData = progressData;

    var enrolmentIds = await _db.CourseEnrolments
        .Where(e => e.StudentId == userId)
        .Select(e => e.CourseId)
        .ToListAsync();
    ViewBag.EnrolledIds = enrolmentIds;
}
```

---

## SECTION 6 — RESPONSIVE DESIGN CHECK

Open the site at each breakpoint and verify nothing is broken.

| Screen Width | Page | What to check |
|---|---|---|
| 1200px | All pages | Full layout as designed |
| 768px | Navbar | Links wrap or truncate cleanly |
| 768px | Course grid | Cards stack to 1 or 2 columns |
| 768px | Forms | Full width, no horizontal scroll |
| 768px | Tables | Wrap inside `.pf-table-wrap`, horizontal scroll appears |
| 480px | Constellation | SVG scrolls horizontally inside `.constellation-wrap` |
| 480px | Stats grid | Collapses to 2 columns |

Browser DevTools → Toggle Device Toolbar (Ctrl+Shift+M) for quick checks.

---

## SECTION 7 — DOCUMENTATION CHECKLIST

Your final report must cover these sections (from the assignment brief).

| Report Section | Contents Required | Done? |
|---|---|---|
| Cover Page | Group members, IDs, intake code, title, dates, APU logo | ☐ |
| Table of Contents | All sections with page numbers | ☐ |
| Introduction | PathForge objectives, scope, project schedule (Gantt) | ☐ |
| Requirement Specification | Audience modelling, use cases, flowcharts | ☐ |
| Design & Modelling | ERD (all 14 tables + relationships), wireframes, navigation structure | ☐ |
| Implementation | CSS examples, form validation explanation, SQL query explanation | ☐ |
| User Guidance | Screenshots of every major page with captions | ☐ |
| Conclusion | Summary, lessons learned, future features | ☐ |
| References | APA 7th edition | ☐ |
| Appendix | Proposal report | ☐ |

**Pages to screenshot for User Guidance (minimum):**
1. Home page (public landing)
2. Register page
3. Login page
4. Course catalogue
5. Course details (enrolled view)
6. Educator — Manage Courses
7. Educator — EditModules
8. Assignment — Submit
9. Quiz — Take
10. Quiz — Result
11. Discussion — Thread
12. Progress — Constellation
13. Admin — Dashboard
14. Profile page

---

## SECTION 8 — SUBMISSION CHECKLIST

Before you zip and upload to Moodle:

- [ ] `dotnet build` — zero errors, zero warnings
- [ ] `dotnet ef database update` — runs clean on a fresh machine
- [ ] All uploaded files (`wwwroot/uploads/`) are NOT committed to source (add to `.gitignore`) — but your report screenshots should show them working
- [ ] `appsettings.json` connection string points to `(localdb)\mssqllocaldb` or the agreed server
- [ ] No hardcoded absolute paths (`C:\Users\...`) anywhere in the code
- [ ] AI Usage Declaration form completed and signed by all members
- [ ] Report is in PDF format, Times New Roman 12pt, 1.5 line spacing, justified
- [ ] Website source code and report both submitted in Moodle by the deadline

---

## ERD SUMMARY (for your report diagram)

```
Users
  ├─< Courses (EducatorId FK)
  ├─< CourseEnrolments (StudentId FK)
  ├─< AssignmentSubmissions (StudentId FK)
  ├─< QuizAttempts (StudentId FK)
  ├─< StudentProgress (StudentId FK)
  ├─< DiscussionPosts (AuthorId FK)
  ├─< ChatMessages (SenderId FK)
  └─< ChatMessages (ReceiverId FK)

Courses
  ├─< Modules
  │     ├─< Materials
  │     └─< StudentProgress (ModuleId FK)
  ├─< CourseEnrolments
  ├─< Assignments
  │     └─< AssignmentSubmissions
  ├─< Quizzes
  │     ├─< QuizQuestions
  │     │     └─< QuizOptions
  │     └─< QuizAttempts
  └─< DiscussionPosts
        └─< DiscussionPosts (self-ref replies, ParentPostId FK)
```

---

## FINAL WORD

Your submission is ready when:

1. Every checkbox in Section 1A is ticked (assignment requirements)
2. Every checkbox in Section 1B is ticked (whiteboard features)
3. Every checkbox in Section 2 is ticked (design system compliance)
4. Every test in Section 3 passes
5. Every item in Section 4 is verified
6. The report matches Section 7

Good luck.
