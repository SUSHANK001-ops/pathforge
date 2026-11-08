# PHASE 1 — PROJECT SETUP, DATABASE SCHEMA & DESIGN SYSTEM
### PathForge · Week 1 of 5

> **Learning reference guide.** Understand every line before you type it.
> Do not paste blindly — your submission must reflect your own understanding.

---

## WHAT YOU BUILD THIS WEEK

- Create the ASP.NET Core MVC project with dotnet CLI
- Define the entire folder structure
- Write all EF Core model classes (your database tables)
- Configure `DbContext` and `Program.cs`
- Build the base `_Layout.cshtml` with the design system
- Write the full `site.css` translating every token from FRAME.md

---

## STEP 1 — CREATE THE PROJECT

Open your terminal in the folder where you want the project.

```bash
dotnet new mvc -n PathForge
cd PathForge

# Add all NuGet packages you will need across all phases
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Session
dotnet add package BCrypt.Net-Next
```

---

## STEP 2 — FULL FOLDER STRUCTURE

After running `dotnet new mvc`, your project will have some files already.
Create or rename folders so it matches exactly this layout:

```
PathForge/
│
├── Controllers/
│   ├── HomeController.cs          ← already exists, keep it
│   ├── AuthController.cs          ← create this
│   ├── CourseController.cs        ← create this
│   ├── MaterialController.cs      ← create this
│   ├── AssignmentController.cs    ← create this
│   ├── QuizController.cs          ← create this
│   ├── DiscussionController.cs    ← create this
│   ├── ProgressController.cs      ← create this
│   └── AdminController.cs         ← create this
│
├── Models/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Course.cs
│   │   ├── Module.cs
│   │   ├── Material.cs
│   │   ├── CourseEnrolment.cs
│   │   ├── Assignment.cs
│   │   ├── AssignmentSubmission.cs
│   │   ├── Quiz.cs
│   │   ├── QuizQuestion.cs
│   │   ├── QuizOption.cs
│   │   ├── QuizAttempt.cs
│   │   ├── StudentProgress.cs
│   │   ├── DiscussionPost.cs
│   │   └── ChatMessage.cs
│   └── ViewModels/
│       ├── LoginViewModel.cs
│       ├── RegisterViewModel.cs
│       ├── CourseViewModel.cs
│       ├── AssignmentViewModel.cs
│       └── QuizViewModel.cs
│
├── Data/
│   └── PathForgeDbContext.cs
│
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   ├── _Navbar.cshtml
│   │   └── Error.cshtml
│   ├── Home/
│   │   └── Index.cshtml
│   ├── Auth/
│   │   ├── Login.cshtml
│   │   ├── Register.cshtml
│   │   └── Profile.cshtml
│   ├── Course/
│   │   ├── Index.cshtml
│   │   ├── Details.cshtml
│   │   ├── Create.cshtml
│   │   └── Edit.cshtml
│   ├── Assignment/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   └── Submit.cshtml
│   ├── Quiz/
│   │   ├── Index.cshtml
│   │   ├── Take.cshtml
│   │   └── Result.cshtml
│   ├── Discussion/
│   │   ├── Index.cshtml
│   │   └── Thread.cshtml
│   ├── Progress/
│   │   └── Index.cshtml
│   └── Admin/
│       ├── Index.cshtml
│       ├── Users.cshtml
│       └── Courses.cshtml
│
├── wwwroot/
│   ├── css/
│   │   ├── site.css            ← your entire design system goes here
│   │   └── constellation.css   ← learning path animation styles
│   ├── js/
│   │   ├── site.js             ← form validation and general JS
│   │   └── constellation.js    ← learning path star animation
│   └── uploads/
│       ├── materials/          ← educator uploaded files
│       └── submissions/        ← student submitted files
│
├── appsettings.json
└── Program.cs
```

---

## STEP 3 — DATABASE SCHEMA

### 3A — Entity Relationship Overview

```
Users ─────────── teaches ──────► Courses
  │                                  │
  │ enrolls ──────────────────────► CourseEnrolments
  │                                  │
  │                               Modules
  │                                  │
  │                               Materials (files/videos)
  │                                  │
  │                               Assignments ──► AssignmentSubmissions
  │                                  │
  │                               Quizzes ──► QuizQuestions ──► QuizOptions
  │                                              │
  │                                           QuizAttempts
  │
  ├── StudentProgress (tracks each module)
  ├── DiscussionPosts (threaded, per course)
  └── ChatMessages (direct messages)
```

### 3B — All Model Classes (C#)

Create each file inside `Models/Entities/`.

**`Models/Entities/User.cs`**
```csharp
using System.ComponentModel.DataAnnotations;

namespace PathForge.Models.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(200), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Role: "Student" | "Educator" | "Admin"
        [Required, MaxLength(20)]
        public string Role { get; set; } = "Student";

        public string? ProfilePicture { get; set; }
        public string? Bio { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Course> CoursesCreated { get; set; } = new List<Course>();
        public ICollection<CourseEnrolment> Enrolments { get; set; } = new List<CourseEnrolment>();
        public ICollection<StudentProgress> ProgressRecords { get; set; } = new List<StudentProgress>();
        public ICollection<DiscussionPost> Posts { get; set; } = new List<DiscussionPost>();
    }
}
```

**`Models/Entities/Course.cs`**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class Course
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        // Status: "Draft" | "Live" | "Archived"
        [MaxLength(20)]
        public string Status { get; set; } = "Draft";

        public string? ThumbnailUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Foreign key
        public int EducatorId { get; set; }
        [ForeignKey("EducatorId")]
        public User Educator { get; set; } = null!;

        // Navigation
        public ICollection<Module> Modules { get; set; } = new List<Module>();
        public ICollection<CourseEnrolment> Enrolments { get; set; } = new List<CourseEnrolment>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
        public ICollection<DiscussionPost> DiscussionPosts { get; set; } = new List<DiscussionPost>();
    }
}
```

**`Models/Entities/Module.cs`**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class Module
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Type: "Lesson" | "Video" | "Lab" | "Reading"
        [MaxLength(30)]
        public string Type { get; set; } = "Lesson";

        // Order controls the constellation sequence
        public int DisplayOrder { get; set; }

        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;

        public ICollection<Material> Materials { get; set; } = new List<Material>();
        public ICollection<StudentProgress> ProgressRecords { get; set; } = new List<StudentProgress>();
    }
}
```

**`Models/Entities/Material.cs`**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class Material
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        // Type: "Document" | "Video" | "Link" | "Slide"
        [MaxLength(30)]
        public string Type { get; set; } = "Document";

        [Required]
        public string FileUrl { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public int ModuleId { get; set; }
        [ForeignKey("ModuleId")]
        public Module Module { get; set; } = null!;
    }
}
```

**`Models/Entities/CourseEnrolment.cs`**
```csharp
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class CourseEnrolment
    {
        public int Id { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.Now;

        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public User Student { get; set; } = null!;

        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;
    }
}
```

**`Models/Entities/Assignment.cs`**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class Assignment
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }
        public int MaxMarks { get; set; } = 100;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;

        public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
    }
}
```

**`Models/Entities/AssignmentSubmission.cs`**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class AssignmentSubmission
    {
        public int Id { get; set; }

        public string? FileUrl { get; set; }
        public string? Comment { get; set; }

        // Status: "Submitted" | "Graded" | "Late"
        [MaxLength(20)]
        public string Status { get; set; } = "Submitted";

        public int? MarksAwarded { get; set; }
        public string? GraderFeedback { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
        public DateTime? GradedAt { get; set; }

        public int AssignmentId { get; set; }
        [ForeignKey("AssignmentId")]
        public Assignment Assignment { get; set; } = null!;

        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public User Student { get; set; } = null!;
    }
}
```

**`Models/Entities/Quiz.cs`**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class Quiz
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
        public int TimeLimitMinutes { get; set; } = 30;
        public int PassMarkPercent { get; set; } = 50;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;

        public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
        public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
    }
}
```

**`Models/Entities/QuizQuestion.cs`**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class QuizQuestion
    {
        public int Id { get; set; }

        [Required]
        public string QuestionText { get; set; } = string.Empty;

        public int Marks { get; set; } = 1;
        public int DisplayOrder { get; set; }

        public int QuizId { get; set; }
        [ForeignKey("QuizId")]
        public Quiz Quiz { get; set; } = null!;

        public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
    }
}
```

**`Models/Entities/QuizOption.cs`**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class QuizOption
    {
        public int Id { get; set; }

        [Required]
        public string OptionText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; } = false;

        public int QuestionId { get; set; }
        [ForeignKey("QuestionId")]
        public QuizQuestion Question { get; set; } = null!;
    }
}
```

**`Models/Entities/QuizAttempt.cs`**
```csharp
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class QuizAttempt
    {
        public int Id { get; set; }
        public int ScorePercent { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public bool Passed { get; set; }
        public DateTime AttemptedAt { get; set; } = DateTime.Now;

        public int QuizId { get; set; }
        [ForeignKey("QuizId")]
        public Quiz Quiz { get; set; } = null!;

        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public User Student { get; set; } = null!;
    }
}
```

**`Models/Entities/StudentProgress.cs`**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class StudentProgress
    {
        public int Id { get; set; }

        // Status: "Locked" | "InProgress" | "Completed"
        [MaxLength(20)]
        public string Status { get; set; } = "Locked";

        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public User Student { get; set; } = null!;

        public int ModuleId { get; set; }
        [ForeignKey("ModuleId")]
        public Module Module { get; set; } = null!;
    }
}
```

**`Models/Entities/DiscussionPost.cs`**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class DiscussionPost
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsFlagged { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Null = root post, not null = reply to another post
        public int? ParentPostId { get; set; }
        [ForeignKey("ParentPostId")]
        public DiscussionPost? ParentPost { get; set; }

        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;

        public int AuthorId { get; set; }
        [ForeignKey("AuthorId")]
        public User Author { get; set; } = null!;

        public ICollection<DiscussionPost> Replies { get; set; } = new List<DiscussionPost>();
    }
}
```

**`Models/Entities/ChatMessage.cs`**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        public string MessageText { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.Now;

        public int SenderId { get; set; }
        [ForeignKey("SenderId")]
        public User Sender { get; set; } = null!;

        public int ReceiverId { get; set; }
        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; } = null!;
    }
}
```

---

## STEP 4 — DATABASE CONTEXT

**`Data/PathForgeDbContext.cs`**
```csharp
using Microsoft.EntityFrameworkCore;
using PathForge.Models.Entities;

namespace PathForge.Data
{
    public class PathForgeDbContext : DbContext
    {
        public PathForgeDbContext(DbContextOptions<PathForgeDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<CourseEnrolment> CourseEnrolments { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizOption> QuizOptions { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<StudentProgress> StudentProgress { get; set; }
        public DbSet<DiscussionPost> DiscussionPosts { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Prevent cascade delete loops on self-referencing DiscussionPost
            modelBuilder.Entity<DiscussionPost>()
                .HasOne(p => p.ParentPost)
                .WithMany(p => p.Replies)
                .HasForeignKey(p => p.ParentPostId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent double cascade on ChatMessage (Sender + Receiver both FK to User)
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique email constraint
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}
```

---

## STEP 5 — appsettings.json

Replace the `ConnectionStrings` section with your SQL Server database name.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PathForgeDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## STEP 6 — Program.cs

```csharp
using Microsoft.EntityFrameworkCore;
using PathForge.Data;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<PathForgeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session (for login state — no ASP Identity)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();          // ← must come before MapControllerRoute
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

---

## STEP 7 — RUN FIRST MIGRATION

```bash
# This creates the database tables from your models
dotnet ef migrations add InitialCreate
dotnet ef database update
```

If successful, you will see `PathForgeDB` in SQL Server Object Explorer.

---

## STEP 8 — DESIGN SYSTEM CSS

This is where the FRAME.md style gets converted to web CSS.
Put everything below inside `wwwroot/css/site.css`.

```css
/* ============================================================
   PATHFORGE DESIGN SYSTEM
   Translated from FRAME.md — Bold Poster editorial style
   Four colors only. Square corners. Red is the lone accent.
   ============================================================ */

/* --- Google Fonts Import --- */
@import url('https://fonts.googleapis.com/css2?family=Shrikhand&family=Libre+Baskerville:ital,wght@0,400;0,700;1,400&family=Space+Grotesk:wght@400;600;700&display=swap');

/* --- Color Tokens --- */
:root {
  --bg:    #FFFFFF;
  --dark:  #1C1410;
  --red:   #D8000F;
  --light: #F5F2EF;

  /* Typography scale (web-adapted from cqw → rem/clamp) */
  --font-display:  'Shrikhand', serif;
  --font-body:     'Libre Baskerville', Georgia, serif;
  --font-chrome:   'Space Grotesk', sans-serif;

  --text-body:          1rem;        /* Baskerville body */
  --text-body-cell:     0.875rem;
  --text-label:         0.625rem;    /* Space Grotesk label — 10px, tracked, uppercase */
  --text-bullet-body:   0.875rem;
  --text-card-title:    1.5rem;      /* Shrikhand card title */
  --text-cell-number:   2rem;        /* Shrikhand cell number in red */
  --text-section-header:2.25rem;     /* Shrikhand section header */
  --text-hero:          clamp(2.5rem, 6vw, 5rem);
  --text-hero-red:      clamp(3rem, 7vw, 6rem);
  --text-stat-big:      clamp(4rem, 12vw, 9rem);

  /* Spacing */
  --pad-page:  3rem 4rem;
  --pad-card:  0 0 0 1.25rem;       /* red leftbar card padding */
  --gap-grid:  1.5rem 2rem;

  /* Borders */
  --border-outer: 3px solid var(--dark);
  --border-inner: 1.5px solid var(--dark);
  --border-leftbar: 4px solid var(--red);

  /* Stacked text shadow (ONLY shadow in the system — text-shadow only) */
  --shadow-stacked:
    2px 2px 0 rgba(28, 20, 16, 0.25),
    4px 4px 0 rgba(28, 20, 16, 0.20),
    6px 6px 0 rgba(28, 20, 16, 0.15);
}

/* --- Reset & Base --- */
*, *::before, *::after {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

html { font-size: 16px; }

body {
  font-family: var(--font-body);
  font-size: var(--text-body);
  font-weight: 400;
  line-height: 1.75;
  color: var(--dark);
  background-color: var(--bg);
  /* NO box-shadow, NO rounded corners, NO gradients */
}

img { max-width: 100%; display: block; }
a { color: var(--red); text-decoration: none; }
a:hover { text-decoration: underline; }


/* ============================================================
   TYPOGRAPHY COMPONENTS
   ============================================================ */

/* Shrikhand Display — hero, section headers, card titles, numbers */
.pf-hero-title {
  font-family: var(--font-display);
  font-size: var(--text-hero);
  font-weight: 400;
  line-height: 0.88;
  color: var(--dark);
}

.pf-hero-title-red {
  font-family: var(--font-display);
  font-size: var(--text-hero-red);
  font-weight: 400;
  line-height: 0.85;
  color: var(--red);
  transform: rotate(-4deg);
  display: inline-block;
}

.pf-section-header {
  font-family: var(--font-display);
  font-size: var(--text-section-header);
  font-weight: 400;
  line-height: 1.0;
  color: var(--dark);
}

.pf-card-title {
  font-family: var(--font-display);
  font-size: var(--text-card-title);
  font-weight: 400;
  line-height: 1.1;
  color: var(--dark);
}

.pf-stat-big {
  font-family: var(--font-display);
  font-size: var(--text-stat-big);
  font-weight: 400;
  line-height: 0.82;
  color: var(--red);
  transform: rotate(-6deg);
  display: inline-block;
}

.pf-cell-number {
  font-family: var(--font-display);
  font-size: var(--text-cell-number);
  font-weight: 400;
  color: var(--red);
  line-height: 1.0;
}

/* Libre Baskerville — all body text */
.pf-body {
  font-family: var(--font-body);
  font-size: var(--text-body);
  font-weight: 400;
  line-height: 1.75;
  color: var(--dark);
  max-width: 65ch;
}

.pf-body-cell {
  font-family: var(--font-body);
  font-size: var(--text-body-cell);
  line-height: 1.55;
}

/* Space Grotesk — labels, eyebrows, bullets, chrome only */
.pf-label {
  font-family: var(--font-chrome);
  font-size: var(--text-label);
  font-weight: 600;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: var(--red);
  display: block;
}

.pf-bullet-body {
  font-family: var(--font-chrome);
  font-size: var(--text-bullet-body);
  font-weight: 400;
  line-height: 1.45;
}


/* ============================================================
   LAYOUT
   ============================================================ */

.pf-page {
  padding: var(--pad-page);
  max-width: 1200px;
  margin: 0 auto;
}

.pf-page--full {
  padding: var(--pad-page);
  width: 100%;
}

/* Off-white alternating section */
.pf-section-light {
  background-color: var(--light);
  padding: 3rem 4rem;
}

.pf-section-dark {
  background-color: var(--dark);
  color: var(--bg);
  padding: 3rem 4rem;
}

.pf-section-red {
  background-color: var(--red);
  color: var(--bg);
  padding: 3rem 4rem;
}


/* ============================================================
   NAVIGATION BAR
   ============================================================ */

.pf-navbar {
  background-color: var(--dark);
  color: var(--bg);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 2.5rem;
  height: 60px;
  border-bottom: 3px solid var(--red);  /* red bottom border — the accent */
  position: sticky;
  top: 0;
  z-index: 100;
}

/* Logo */
.pf-navbar__brand {
  font-family: var(--font-display);
  font-size: 1.4rem;
  font-weight: 400;
  color: var(--bg);
  text-decoration: none;
  letter-spacing: 0;
}

.pf-navbar__brand span {
  color: var(--red);  /* "PF" mark in red */
}

/* Nav links */
.pf-navbar__links {
  display: flex;
  align-items: center;
  gap: 2rem;
  list-style: none;
}

.pf-navbar__links a {
  font-family: var(--font-chrome);
  font-size: 0.7rem;
  font-weight: 600;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: var(--light);
  text-decoration: none;
  padding: 0.25rem 0;
  border-bottom: 2px solid transparent;
  transition: border-color 0.15s ease;
}

.pf-navbar__links a:hover,
.pf-navbar__links a.active {
  color: var(--bg);
  border-bottom-color: var(--red);
  text-decoration: none;
}

/* Role badge in navbar */
.pf-navbar__role {
  font-family: var(--font-chrome);
  font-size: 0.6rem;
  font-weight: 600;
  letter-spacing: 1.5px;
  text-transform: uppercase;
  color: var(--light);
  opacity: 0.6;
}


/* ============================================================
   BUTTONS
   ============================================================ */

/* Primary — red fill, Space Grotesk, NO rounded corners */
.pf-btn {
  display: inline-block;
  font-family: var(--font-chrome);
  font-size: 0.75rem;
  font-weight: 600;
  letter-spacing: 1.5px;
  text-transform: uppercase;
  color: var(--bg);
  background-color: var(--red);
  border: 2px solid var(--red);
  border-radius: 0;              /* ← FRAME.md: square corners always */
  padding: 0.6rem 1.4rem;
  cursor: pointer;
  text-decoration: none;
  transition: background-color 0.1s ease, color 0.1s ease;
}

.pf-btn:hover {
  background-color: var(--dark);
  border-color: var(--dark);
  color: var(--bg);
  text-decoration: none;
}

/* Ghost button — outline only */
.pf-btn--ghost {
  background-color: transparent;
  color: var(--dark);
  border-color: var(--dark);
}

.pf-btn--ghost:hover {
  background-color: var(--dark);
  color: var(--bg);
}

/* Small variant */
.pf-btn--sm {
  font-size: 0.65rem;
  padding: 0.35rem 0.85rem;
  letter-spacing: 1px;
}

/* Danger — same structure, just darker red awareness */
.pf-btn--danger {
  background-color: var(--dark);
  border-color: var(--red);
  color: var(--red);
}

.pf-btn--danger:hover {
  background-color: var(--red);
  color: var(--bg);
}


/* ============================================================
   FORMS & INPUTS
   ============================================================ */

.pf-form-group {
  margin-bottom: 1.5rem;
}

/* Form label — Space Grotesk uppercase in red */
.pf-form-label {
  display: block;
  font-family: var(--font-chrome);
  font-size: 0.6rem;
  font-weight: 600;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: var(--red);
  margin-bottom: 0.4rem;
}

/* Input field — dark border, square corners, Baskerville text */
.pf-input {
  display: block;
  width: 100%;
  font-family: var(--font-body);
  font-size: 0.9rem;
  color: var(--dark);
  background-color: var(--bg);
  border: 2px solid var(--dark);
  border-radius: 0;              /* ← FRAME.md: square only */
  padding: 0.6rem 0.75rem;
  outline: none;
  transition: border-color 0.15s ease;
}

.pf-input:focus {
  border-color: var(--red);      /* red focus ring — accent rule */
}

.pf-input::placeholder {
  color: var(--dark);
  opacity: 0.35;
  font-style: italic;
}

.pf-select {
  appearance: none;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='8' viewBox='0 0 12 8'%3E%3Cpath d='M1 1l5 5 5-5' stroke='%231C1410' stroke-width='2' fill='none'/%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: right 0.75rem center;
  padding-right: 2.5rem;
}

/* Textarea */
.pf-textarea {
  resize: vertical;
  min-height: 120px;
}

/* Validation error text */
.pf-field-error {
  font-family: var(--font-chrome);
  font-size: 0.65rem;
  font-weight: 600;
  letter-spacing: 1px;
  text-transform: uppercase;
  color: var(--red);
  margin-top: 0.3rem;
  display: block;
}

/* Form container — bordered panel */
.pf-form-panel {
  border: var(--border-outer);
  padding: 2rem 2.5rem;
  background-color: var(--bg);
  max-width: 480px;
  width: 100%;
}

.pf-form-panel__header {
  border-bottom: var(--border-inner);
  padding-bottom: 1rem;
  margin-bottom: 1.5rem;
}


/* ============================================================
   CARDS — RED LEFTBAR (editorial card cantilevered off red rule)
   ============================================================ */

.pf-card {
  border-left: var(--border-leftbar);   /* 4px solid red — the signature */
  padding: var(--pad-card);
  background-color: var(--bg);
  border-radius: 0;                      /* FRAME.md: no rounded corners */
  /* NO box-shadow */
}

.pf-card--outlined {
  border: var(--border-outer);
  border-left: var(--border-leftbar);
  padding: 1.25rem 1.5rem 1.25rem 1.25rem;
}

.pf-card--light {
  background-color: var(--light);
}

/* Course card */
.pf-course-card {
  border: var(--border-outer);
  border-left: var(--border-leftbar);
  padding: 1.25rem 1.5rem 1.25rem 1.25rem;
  background-color: var(--bg);
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.pf-course-card__category {
  font-family: var(--font-chrome);
  font-size: 0.6rem;
  font-weight: 600;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: var(--red);
}

.pf-course-card__title {
  font-family: var(--font-display);
  font-size: 1.35rem;
  font-weight: 400;
  color: var(--dark);
  line-height: 1.15;
}

.pf-course-card__meta {
  font-family: var(--font-chrome);
  font-size: 0.7rem;
  color: var(--dark);
  opacity: 0.65;
}

.pf-course-card__actions {
  display: flex;
  gap: 0.5rem;
  margin-top: auto;
  padding-top: 1rem;
  border-top: var(--border-inner);
}

/* Status pill — the ONLY 4px border-radius in the system */
.pf-status {
  display: inline-block;
  font-family: var(--font-chrome);
  font-size: 0.55rem;
  font-weight: 600;
  letter-spacing: 1.5px;
  text-transform: uppercase;
  padding: 0.15rem 0.5rem;
  border-radius: 4px;             /* FRAME.md: hint pill exception */
  border: 1px solid currentColor;
}

.pf-status--live    { color: var(--red); border-color: var(--red); }
.pf-status--draft   { color: var(--dark); opacity: 0.5; }
.pf-status--archived{ color: var(--dark); opacity: 0.4; }


/* ============================================================
   DATA TABLE — DOUBLE BORDER GRID (fin-grid signature)
   ============================================================ */

.pf-table-wrap {
  border: var(--border-outer);   /* 3px outer border */
  overflow-x: auto;
}

.pf-table {
  width: 100%;
  border-collapse: collapse;
  background-color: var(--bg);
}

.pf-table thead {
  background-color: var(--dark);
  color: var(--bg);
}

.pf-table thead th {
  font-family: var(--font-chrome);
  font-size: 0.6rem;
  font-weight: 600;
  letter-spacing: 2px;
  text-transform: uppercase;
  padding: 0.75rem 1rem;
  text-align: left;
  border-right: 1.5px solid rgba(245, 242, 239, 0.2);  /* inner border on dark bg */
}

.pf-table thead th:last-child { border-right: none; }

.pf-table tbody tr {
  border-bottom: var(--border-inner);  /* 1.5px inner horizontal rule */
}

.pf-table tbody tr:nth-child(even) {
  background-color: var(--light);      /* alternating light stripe */
}

.pf-table tbody tr:hover {
  background-color: #ede9e5;
}

.pf-table tbody td {
  font-family: var(--font-body);
  font-size: var(--text-body-cell);
  padding: 0.7rem 1rem;
  color: var(--dark);
  border-right: var(--border-inner);   /* 1.5px inner vertical rule */
}

.pf-table tbody td:last-child { border-right: none; }

/* Number cell — red Shrikhand */
.pf-table .cell-number {
  font-family: var(--font-display);
  font-size: 1.25rem;
  color: var(--red);
  font-weight: 400;
}


/* ============================================================
   PROGRESS BAR — red bottom strip (most prominent chrome)
   ============================================================ */

.pf-progress {
  width: 100%;
  height: 8px;
  background-color: var(--light);
  border: 1px solid var(--dark);
  border-radius: 0;
}

.pf-progress__fill {
  height: 100%;
  background-color: var(--red);
  border-radius: 0;
  transition: width 0.3s ease;
}

/* Page-bottom strip version */
.pf-progress-strip {
  position: fixed;
  bottom: 0;
  left: 0;
  height: 4px;
  background-color: var(--red);
  z-index: 999;
  transition: width 0.3s ease;
}


/* ============================================================
   STAT PANELS — for homepage and admin dashboard
   ============================================================ */

.pf-stat-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 0;
  border: var(--border-outer);
}

.pf-stat-cell {
  padding: 1.5rem;
  border-right: var(--border-inner);
  border-bottom: var(--border-inner);
}

.pf-stat-cell:last-child { border-right: none; }

.pf-stat-cell__number {
  font-family: var(--font-display);
  font-size: 2.5rem;
  font-weight: 400;
  color: var(--red);
  line-height: 1.0;
  display: block;
}

.pf-stat-cell__label {
  font-family: var(--font-chrome);
  font-size: 0.6rem;
  font-weight: 600;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: var(--dark);
  opacity: 0.6;
  display: block;
  margin-top: 0.35rem;
}


/* ============================================================
   ALERTS & MESSAGES
   ============================================================ */

.pf-alert {
  font-family: var(--font-chrome);
  font-size: 0.7rem;
  font-weight: 600;
  letter-spacing: 1px;
  text-transform: uppercase;
  padding: 0.7rem 1rem;
  border-left: 4px solid var(--red);
  background-color: var(--light);
  color: var(--dark);
  border-radius: 0;
}

.pf-alert--success {
  border-left-color: var(--dark);
}

.pf-alert--error {
  border-left-color: var(--red);
  background-color: #fff0f0;
}


/* ============================================================
   BULLET LIST (red em-dash, max 3 bullets per FRAME.md)
   ============================================================ */

.pf-bullets {
  list-style: none;
  padding: 0;
  margin: 1rem 0;
}

.pf-bullets li {
  font-family: var(--font-chrome);
  font-size: var(--text-bullet-body);
  font-weight: 400;
  line-height: 1.45;
  padding-left: 1.5rem;
  position: relative;
  margin-bottom: 0.5rem;
}

.pf-bullets li::before {
  content: "—";                  /* red em-dash bullet */
  color: var(--red);
  font-weight: 700;
  position: absolute;
  left: 0;
}


/* ============================================================
   RESPONSIVE
   ============================================================ */

@media (max-width: 768px) {
  .pf-page { padding: 1.5rem; }
  .pf-section-light,
  .pf-section-dark,
  .pf-section-red { padding: 2rem 1.5rem; }

  .pf-navbar { padding: 0 1.25rem; }
  .pf-navbar__links { gap: 1rem; }
  .pf-navbar__links a { font-size: 0.6rem; }

  :root {
    --text-hero:     clamp(2rem, 10vw, 3rem);
    --text-hero-red: clamp(2.5rem, 12vw, 3.5rem);
    --text-stat-big: clamp(3rem, 18vw, 5rem);
  }

  .pf-stat-grid { grid-template-columns: 1fr 1fr; }
  .pf-form-panel { max-width: 100%; padding: 1.25rem; }
}
```

---

## STEP 9 — BASE LAYOUT (_Layout.cshtml)

Replace the contents of `Views/Shared/_Layout.cshtml`:

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>@ViewData["Title"] — PathForge</title>
  <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
  @await RenderSectionAsync("Styles", required: false)
</head>
<body>

  @* Render the shared navbar partial *@
  @await Html.PartialAsync("_Navbar")

  <main>
    @RenderBody()
  </main>

  @* Red progress strip at the bottom — the FRAME.md chrome *@
  <div class="pf-progress-strip" id="readProgress" style="width:0%"></div>

  <script src="~/js/site.js" asp-append-version="true"></script>
  @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

---

## STEP 10 — NAVBAR PARTIAL (Views/Shared/_Navbar.cshtml)

```html
@{
  var role     = Context.Session.GetString("UserRole") ?? "";
  var name     = Context.Session.GetString("UserName") ?? "";
  var isLogged = !string.IsNullOrEmpty(role);
}

<nav class="pf-navbar">
  <a href="/" class="pf-navbar__brand">
    <span>PF</span> PathForge
  </a>

  <ul class="pf-navbar__links">
    <li><a href="/">Home</a></li>
    <li><a href="/Course">Courses</a></li>

    @if (!isLogged)
    {
      <li><a href="/Auth/Login">Log In</a></li>
      <li><a href="/Auth/Register" class="pf-btn pf-btn--sm">Register</a></li>
    }
    else
    {
      @if (role == "Educator" || role == "Admin")
      {
        <li><a href="/Course/Manage">Manage Courses</a></li>
      }
      @if (role == "Admin")
      {
        <li><a href="/Admin">Admin</a></li>
      }
      <li>
        <span class="pf-navbar__role">@name · @role</span>
      </li>
      <li><a href="/Auth/Logout">Log Out</a></li>
    }
  </ul>
</nav>
```

---

## WEEK 1 DONE — CHECKLIST

Before moving to Phase 2, verify:

- [ ] `dotnet build` runs with no errors
- [ ] Migration created and database exists in SQL Server
- [ ] All model files exist in `Models/Entities/`
- [ ] `site.css` loads and fonts show (open browser, check Network tab)
- [ ] Navbar renders on the home page with correct fonts
- [ ] Home page shows without errors

---

## WHAT PHASE 2 COVERS

Phase 2 builds the full authentication system:
registration, login, logout, profile management, and role-based access control.
