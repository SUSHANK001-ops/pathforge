# PathForge — Learning Constellation Platform
## Phase 1: Project Setup, Database Schema & Design System Architecture

---

## 1. Project Overview

**PathForge** is an educational platform designed around the concept of **"Learning Constellations"** — nonlinear and structured visual learning paths for students and educators. 

Phase 1 establishes the bedrock architecture of the platform:
- **Framework:** ASP.NET Core MVC on **.NET 10.0**
- **ORM & Data Access:** Entity Framework Core 10 (Code-First)
- **Styling Engine:** **Tailwind CSS v4** coupled with a high-contrast editorial poster design system (translating the FRAME.md aesthetic specification)
- **State Management:** Session-based authentication & state tracking (lightweight, zero ASP.NET Identity bloat)
- **Security:** `BCrypt.Net-Next` for salted password hashing

---

## 2. Directory Structure

The project strictly follows an enterprise separation of concerns:

```text
PathForge/
│
├── Controllers/                  ← MVC Controllers handling HTTP requests & routing
│   ├── HomeController.cs         ← Editorial landing page & entry point
│   ├── AuthController.cs         ← Login, Register, Logout, Profile actions
│   ├── CourseController.cs       ← Course catalog, details, creation & editing
│   ├── MaterialController.cs     ← File and document asset handlers
│   ├── AssignmentController.cs   ← Deliverables, assignment prompts, and submissions
│   ├── QuizController.cs         ← Assessment delivery, timers, and scoring
│   ├── DiscussionController.cs   ← Threaded course forums & replies
│   ├── ProgressController.cs     ← Constellation completion & student tracker
│   └── AdminController.cs        ← Platform oversight, user & course administration
│
├── Models/
│   ├── Entities/                 ← 14 EF Core Database Model classes (Data schema)
│   │   ├── User.cs               ← User accounts & role flags (Student, Educator, Admin)
│   │   ├── Course.cs             ← Learning constellation root entity
│   │   ├── Module.cs             ← Constellation node (Lesson, Video, Lab, Reading)
│   │   ├── Material.cs           ← Resources attached to modules (Docs, Videos, Links)
│   │   ├── CourseEnrolment.cs    ← Student-to-course junction entity
│   │   ├── Assignment.cs         ← Course assignments & deadlines
│   │   ├── AssignmentSubmission.cs ← Student submissions, grading & feedback
│   │   ├── Quiz.cs               ← Assessments with timers & pass marks
│   │   ├── QuizQuestion.cs       ← Individual quiz questions
│   │   ├── QuizOption.cs         ← Multiple choice options with correctness flag
│   │   ├── QuizAttempt.cs        ← Attempt logs, scores & pass/fail records
│   │   ├── StudentProgress.cs    ← Node status tracking (Locked/InProgress/Completed)
│   │   ├── DiscussionPost.cs     ← Threaded discussion forum posts (self-referencing)
│   │   └── ChatMessage.cs        ← Direct user-to-user messaging
│   │
│   └── ViewModels/               ← Strongly-typed models for Razor views & validation
│       ├── LoginViewModel.cs
│       ├── RegisterViewModel.cs
│       ├── CourseViewModel.cs
│       ├── AssignmentViewModel.cs
│       └── QuizViewModel.cs
│
├── Data/
│   ├── PathForgeDbContext.cs     ← EF Core DbContext with Fluent API configurations
│   └── InitialCreate.sql         ← Pre-compiled SQL DDL script for database deployment
│
├── Migrations/                   ← EF Core Migration history files
│   ├── 20260908162736_InitialCreate.cs
│   ├── 20260908162736_InitialCreate.Designer.cs
│   └── PathForgeDbContextModelSnapshot.cs
│
├── Styles/
│   └── input.css                 ← Tailwind CSS v4 input with @theme and poster design tokens
│
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml        ← Master layout with typography, progress strip & partials
│   │   ├── _Navbar.cshtml        ← Dynamic session-aware navigation bar
│   │   └── Error.cshtml
│   ├── Home/Index.cshtml         ← High-impact editorial hero & constellation showcase
│   ├── Auth/                     ← Login, Register, Profile views
│   ├── Course/                   ← Catalog, Details, Create, Edit views
│   ├── Assignment/               ← Deliverables list, Create, and Submit views
│   ├── Quiz/                     ← Catalog, Timed Assessment, and Result views
│   ├── Discussion/               ← Thread listings and reply post views
│   ├── Progress/                 ← Student completion dashboard
│   └── Admin/                    ← Admin metrics, user list, and course management
│
├── wwwroot/
│   ├── css/
│   │   ├── site.css              ← Compiled, minified Tailwind CSS v4 output
│   │   └── constellation.css     ← Constellation canvas node styles & pulsing keyframes
│   ├── js/
│   │   ├── site.js               ← Reading progress strip & client utilities
│   │   └── constellation.js      ← HTML5 Canvas interactive constellation background
│   └── uploads/
│       ├── materials/            ← Dedicated directory for educator uploads
│       └── submissions/          ← Dedicated directory for student deliverables
│
├── appsettings.json              ← SQL Server ConnectionString & logging configuration
├── package.json                  ← Tailwind CSS v4 compilation script configuration
├── PathForge.csproj              ← .NET 10 project definition & NuGet dependencies
└── Program.cs                    ← Middleware pipeline, Session, DbContext & routing
```

---

## 3. Database Architecture & Schema Explained

### Entity Relationship Diagram (ERD)

```text
       ┌──────────┐
       │   User   │◄─────────────────┐
       └────┬─────┘                  │
            │ 1                      │ (Educator)
            │                        │
            ▼ *                      │ *
       ┌──────────┐ 1              * │
       │  Course  ├──────────────────┤
       └────┬─────┘                  │
            │ 1                      │
   ┌────────┴────────┬───────────────┼───────────────┐
   │ 1               │ 1             │ 1             │ 1
   ▼ *               ▼ *             ▼ *             ▼ *
┌──────┐       ┌────────────┐   ┌─────────┐   ┌─────────────────┐
│Module│       │ CourseEnrol│   │  Quiz   │   │   Assignment    │
└──┬───┘       └────────────┘   └────┬────┘   └────────┬────────┘
   │ 1                               │ 1               │ 1
   ├───────────────┐                 ▼ *               ▼ *
   ▼ *             ▼ *          ┌─────────┐   ┌─────────────────┐
┌────────┐ ┌───────────────┐    │QuizQuest│   │AssignmentSubmiss│
│Material│ │StudentProgress│    └────┬────┘   └─────────────────┘
└────────┘ └───────────────┘         │ 1
                                     ▼ *
                                ┌─────────┐
                                │QuizOptn │
                                └─────────┘

Independent Cross-Entity Tables:
- DiscussionPost: CourseId FK, AuthorId FK, Self-referencing ParentPostId FK (for nested replies)
- ChatMessage: SenderId FK, ReceiverId FK (user-to-user direct messages)
```

### Table-by-Table Breakdown

1. **`Users`**: Holds credentials, roles (`Student`, `Educator`, `Admin`), bio, and profile details. Email has a unique database index.
2. **`Courses`**: Learning paths created by educators with status (`Draft`, `Live`, `Archived`).
3. **`Modules`**: Individual learning steps within a course. Features a `DisplayOrder` property to control visual sequence.
4. **`Materials`**: Uploaded assets (Documents, Videos, Links, Slides) with file size metadata.
5. **`CourseEnrolments`**: Connects students to courses they are studying.
6. **`Assignments`**: Course work deadlines, maximum marks, and instructions.
7. **`AssignmentSubmissions`**: Student submissions, grader comments, marks awarded, and submission timestamps.
8. **`Quizzes`**: Knowledge checkpoints with time limits and pass percentages.
9. **`QuizQuestions`**: Questions linked to quizzes with point weightings.
10. **`QuizOptions`**: Multiple-choice options flagged with `IsCorrect`.
11. **`QuizAttempts`**: Audit trail of every student attempt, score percentage, and pass/fail outcome.
12. **`StudentProgress`**: Tracks per-module status (`Locked`, `InProgress`, `Completed`).
13. **`DiscussionPosts`**: Threaded discussion forum supporting nested replies through a self-referencing `ParentPostId`.
14. **`ChatMessages`**: Instant messaging logs between pairs of users.

### Critical Database Integrity Rules in `PathForgeDbContext`

To avoid common SQL Server cyclic cascade issues:
- **`DiscussionPost` Self-Reference:** Configured with `DeleteBehavior.Restrict` so deleting a parent post does not cause a circular cascade lock.
- **`ChatMessage` Dual Foreign Keys:** Both `Sender` and `Receiver` link to `User`. Both are configured with `DeleteBehavior.Restrict` to prevent multiple cascade paths.
- **`User.Email`:** Configured with a database-level unique index.

---

## 4. Design System & Tailwind CSS v4 Integration

The front-end design is translated from **FRAME.md** — a bold poster editorial style:

### 1. Color Palette (Strict 4-Color Discipline)
- **Background (`--bg` / `--color-pf-bg`):** `#FFFFFF`
- **Dark Neutral (`--dark` / `--color-pf-dark`):** `#1C1410` (Deep Espresso Charcoal)
- **Accent Red (`--red` / `--color-pf-red`):** `#D8000F` (Flame Red — lone accent)
- **Editorial Cream (`--light` / `--color-pf-light`):** `#F5F2EF` (Off-white section tint)

### 2. Typography Hierarchy
- **Display / Headers:** `Shrikhand` (chunky, expressive poster headline typeface)
- **Body:** `Libre Baskerville` (traditional editorial serif for high readability)
- **Chrome / Labels / Buttons:** `Space Grotesk` (tracked, uppercase geometric sans-serif)

### 3. Geometric Rules
- **No rounded corners** on standard containers, buttons, cards, or inputs (`border-radius: 0`).
- **Hint Pill Exception:** A single 4px rounded radius is permitted strictly for status pills (`.pf-status`).
- **Signature Components:**
  - **Red Leftbar Cards (`.pf-card`):** 4px solid flame-red cantilever border.
  - **Double-Border Data Table (`.pf-table`):** 3px outer dark border, 1.5px inner grid rules.
  - **Reading Progress Strip (`#readProgress`):** Fixed 4px red bottom bar responding dynamically to window scroll.
  - **Angled Display Accents:** `-4deg` and `-6deg` transforms on hero words and stat highlights.

---

## 5. Middleware & Pipeline Configuration (`Program.cs`)

1. **`AddControllersWithViews()`**: Standard MVC support.
2. **`AddDbContext<PathForgeDbContext>()`**: Registers SQL Server provider.
3. **`AddSession()`**:
   - Idle timeout: 2 hours
   - `Cookie.HttpOnly = true` (mitigates XSS cookie theft)
   - `Cookie.IsEssential = true` (preserves login across cookie consent prompts)
4. **Middleware Execution Order:**
   - `UseHttpsRedirection()`
   - `UseStaticFiles()`
   - `UseRouting()`
   - **`UseSession()`** (Placed strictly before `MapControllerRoute` so controllers can read session roles)
   - `UseAuthorization()`
   - `MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")`

---

## 6. How to Build & Run

### Prerequisites
- .NET 10.0 SDK (`dotnet --version` outputs `10.0.400`)
- Node.js 20+ & npm (`node -v` and `npm -v`)

### 1. Compile CSS (Tailwind v4)
```powershell
# From the PathForge directory:
npm run build:css
```
*(Or run `npm run watch:css` while actively developing styling.)*

### 2. Build the Application
```powershell
dotnet build
```

### 3. Apply the Database Migration
The migration code (`InitialCreate`) is generated. When SQL Server (LocalDB or SQL Server Express) is active, execute:
```powershell
dotnet ef database update
```
*Alternatively, you can open and execute `PathForge/Data/InitialCreate.sql` directly inside SQL Server Management Studio (SSMS) or Azure Data Studio.*

### 4. Run the Web Application
```powershell
dotnet run
```
Navigate to:
- **`http://localhost:5280`** or **`https://localhost:7246`**

---

## 7. Week 1 Verification Checklist Status

- [x] Project created with .NET CLI (`dotnet new mvc -n PathForge` on .NET 10)
- [x] All required NuGet packages added (`EFCore.SqlServer`, `EFCore.Tools`, `BCrypt.Net-Next`)
- [x] All 14 model entity files created in `Models/Entities/`
- [x] ViewModels created in `Models/ViewModels/`
- [x] `PathForgeDbContext` configured with all DbSets, Restrict delete behaviors, and email uniqueness
- [x] `appsettings.json` configured with SQL Server connection string
- [x] `Program.cs` configured with Session, DbContext, and middleware
- [x] Tailwind CSS v4 installed, configured in `Styles/input.css`, and compiled to `wwwroot/css/site.css`
- [x] Google Fonts (`Shrikhand`, `Libre Baskerville`, `Space Grotesk`) loaded and verified
- [x] `_Layout.cshtml` and `_Navbar.cshtml` created with dynamic session state checks
- [x] Baseline views and controllers created for all routes
- [x] EF Core migration `InitialCreate` and standalone DDL script `Data/InitialCreate.sql` generated
- [x] Application successfully compiles (`0 Errors`) and serves HTTP 200 on `http://localhost:5280`

---

## 8. Looking Ahead to Phase 2

**Phase 2** will implement the full user authentication and security layer:
1. User registration with BCrypt password hashing.
2. Secure session-based login with role distinction (`Student` vs `Educator` vs `Admin`).
3. Role-based authorization filters (`[RequireRole("Educator")]`).
4. Profile page management and avatar upload handling.
