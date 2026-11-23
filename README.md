# PathForge — Learning Constellation Platform

> A web-based Learning Management System (LMS) built for the CT050-3-2-WAPP
> module. Students enrol in courses, learn through modules, build in an
> in-browser **Virtual Lab**, take quizzes, submit assignments, discuss,
> track progress on a visual **constellation**, and earn **certificates**.
> Educators author everything; administrators oversee the platform.

This document is the complete reference for the project. A separate
[**SETUP.md**](SETUP.md) explains how to install, run, and keep the database
running across laptop restarts.

---

## Table of Contents
1. [Technology Stack](#1-technology-stack)
2. [User Roles](#2-user-roles)
3. [Feature Catalogue](#3-feature-catalogue)
4. [Database Schema (16 Tables)](#4-database-schema-16-tables)
5. [Entity Relationship Diagram](#5-entity-relationship-diagram)
6. [Controllers & Routes](#6-controllers--routes)
7. [Views (Pages)](#7-views-pages)
8. [Design System (FRAME.md)](#8-design-system-framemd)
9. [Security](#9-security)
10. [Seeded Demo Data & Test Accounts](#10-seeded-demo-data--test-accounts)
11. [Project Structure](#11-project-structure)
12. [Assignment Requirement Mapping](#12-assignment-requirement-mapping)

---

## 1. Technology Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core **MVC** on **.NET 10.0** |
| Data access | **Entity Framework Core 10** (Code-First, migrations) |
| Database | **SQL Server Express** (instance `.\SQLEXPRESS01`) |
| Authentication | **Session-based** (no ASP.NET Identity); passwords hashed with **BCrypt.Net-Next** |
| Styling | Custom CSS design system (FRAME.md), Tailwind-generated base, three Google Fonts |
| Client scripting | Vanilla JavaScript (form validation, reading-progress strip, constellation canvas, live lab preview) |

---

## 2. User Roles

| Role | Can do |
|---|---|
| **Student** | Browse/enrol in courses, work through modules, open Virtual Labs, take quizzes, submit assignments, post in discussions, read announcements, track progress on the constellation, earn and print certificates. |
| **Educator** | Create/publish/archive courses, add modules and materials, author Virtual Labs, create quizzes and assignments, grade submissions, post announcements, moderate their own course discussions. |
| **Administrator** | Everything an educator can do on any course, plus manage all users (suspend/reactivate), review all courses, and see platform-wide stats. |

Roles are stored on the `User` entity and enforced server-side via
`SessionHelper.RequireRole(...)` in every protected controller action.

---

## 3. Feature Catalogue

### Core learning
- **Course catalogue** — public list of Live courses with search and category filters. Enrolled students see a progress bar and a "Continue" button on each card.
- **Course details** — hero, educator, enrolment count, and the ordered module list. Materials unlock on enrolment.
- **Modules** — four types: `Lesson`, `Video`, `Lab`, `Reading`, ordered by `DisplayOrder`.
- **Materials** — `Document`, `Slide`, `Video`, or external `Link` attached to a module (file upload or URL).
- **Enrolment** — one click; auto-creates per-module progress rows (module 1 `InProgress`, the rest `Locked`).

### Virtual Lab (interactive, creative feature)
- An **in-browser HTML/CSS/JS sandbox** attached to a `Lab` module.
- Split editor (HTML / CSS / JS) with a **live preview** rendered in a sandboxed `<iframe>` (`sandbox="allow-scripts"`, `srcdoc`), so student code runs client-side only — never on the server.
- Educators define **starter code** and **expected markers** (case-insensitive snippets the student's combined code must contain).
- **Save & Run Checks** verifies the markers; when all pass, the lab module is marked **Completed**, the next module unlocks, and a certificate is issued if the course is now 100%.
- Student work is saved per lab and reloads where they left off.

### Assessment
- **Quizzes** — timed, multiple-choice, auto-scored with a pass mark; attempts logged with score and pass/fail. Optional browser countdown.
- **Assignments** — file/comment submission with due dates; status is `Submitted`, `Late`, or `Graded`; educators award marks and written feedback.

### Progress & achievements
- **Constellation** — SVG star map of a course: completed nodes are red, in-progress is dark (pulsing), locked is grey. Marking a module complete unlocks the next.
- **Certificates** — issued automatically the first time a student reaches 100% in a course. A printable certificate page (`window.print()` → PDF) shows the student name, course, educator, issue date, and a unique serial code.

### Communication
- **Discussions** — threaded course forums with replies (self-referencing posts). Authors delete their own posts; admins delete any.
- **Announcements** — course-wide notices posted by educators/admins, pinnable, shown on the student dashboard and the course's announcements page.

### Platform & usability
- **Role-aware dashboard** (`/`) — logged-in home. Students see continue-learning cards with progress, due-soon assignments, recent announcements, and achievement stats. Educators/Admins see their courses with "to grade" badges and student counts.
- **Global search** — navbar search across Live courses and discussion threads (`/Home/Search`).
- **Profile management** — edit name, bio, and change password.
- **Admin panel** — platform stats, user management (suspend/reactivate), all-courses view.
- **Breadcrumbs** on inner pages; **reading-progress strip** at the bottom of every page.

---

## 4. Database Schema (16 Tables)

| # | Table | Purpose |
|---|---|---|
| 1 | **Users** | Accounts, role, bio, `IsActive`, BCrypt `PasswordHash`. Unique index on `Email`. |
| 2 | **Courses** | Course root; `Status` = Draft/Live/Archived; FK `EducatorId`. |
| 3 | **Modules** | Ordered learning steps; `Type` = Lesson/Video/Lab/Reading; FK `CourseId`. |
| 4 | **Materials** | Resources on a module; `Type` = Document/Slide/Video/Link. |
| 5 | **CourseEnrolments** | Student ↔ course junction. |
| 6 | **Assignments** | Course work with due date and max marks. |
| 7 | **AssignmentSubmissions** | Student submissions, status, marks, feedback. |
| 8 | **Quizzes** | Time limit, pass-mark percent. |
| 9 | **QuizQuestions** | Questions with marks and order. |
| 10 | **QuizOptions** | Options with `IsCorrect` flag. |
| 11 | **QuizAttempts** | Score, correct count, pass/fail log. |
| 12 | **StudentProgress** | Per-module status: Locked/InProgress/Completed. |
| 13 | **DiscussionPosts** | Threaded forum; self-referencing `ParentPostId`. |
| 14 | **ChatMessages** | User-to-user direct messages (schema present). |
| 15 | **VirtualLabs** | Lab brief, starter code, expected markers; one per module. |
| 16 | **VirtualLabSubmissions** | Student lab code + check results; one per student per lab. |
| + | **Announcements** | Course notices (educator/admin → enrolled students). |
| + | **Certificates** | Completion certificates; one per student per course. |

> The migration creates **18** entity tables plus `__EFMigrationsHistory`.

### Cascade-path rules (`PathForgeDbContext.OnModelCreating`)
SQL Server rejects multiple cascade paths to the same table, so every
foreign key that points to `Users` through a second route is configured with
`DeleteBehavior.Restrict`: `CourseEnrolment`, `AssignmentSubmission`,
`QuizAttempt`, `StudentProgress`, `DiscussionPost` (Author),
`VirtualLabSubmission`, `Announcement` (Author), `Certificate` (Student),
plus both `ChatMessage` FKs and the self-referencing `DiscussionPost.ParentPost`.
Unique indexes: `User.Email`, `VirtualLab.ModuleId`,
`VirtualLabSubmission(VirtualLabId, StudentId)`, `Certificate(StudentId, CourseId)`.

---

## 5. Entity Relationship Diagram

```text
                         ┌───────────┐
          (Educator) ───►│   Users   │◄─── (Author / Student)
                         └─────┬─────┘
                               │ 1
        ┌──────────────────────┼───────────────────────────────┐
        │ *                    │ *                              │ *
  ┌───────────┐          ┌──────────────┐               ┌───────────────┐
  │  Courses  │          │CourseEnrolmnt│               │  Certificates │
  └─────┬─────┘          └──────────────┘               └───────────────┘
        │ 1
  ┌─────┼───────────┬──────────────┬───────────────┬──────────────┐
  ▼ *   ▼ *         ▼ *            ▼ *             ▼ *            ▼ *
┌──────┐┌─────────┐┌──────────┐┌──────────────┐┌─────────────┐┌──────────────┐
│Module││  Quiz   ││Assignment││DiscussionPost││Announcements││ (Enrolments) │
└──┬───┘└────┬────┘└────┬─────┘└──────┬───────┘└─────────────┘└──────────────┘
   │ 1       │ 1        │ 1           │ self-ref replies
   ├─────┐   ▼ *        ▼ *           (ParentPostId)
   ▼ *   ▼ * ┌─────────┐┌──────────────────┐
┌────────┐  │QuizQuest││AssignmentSubmiss │
│Material│  └────┬────┘└──────────────────┘
└────────┘       ▼ *
┌───────────────┐┌─────────┐
│StudentProgress││QuizOptns│
└───────────────┘└─────────┘
   ▲ 1 (per module)
   │
┌──────────┐     ┌──────────────────────┐
│VirtualLab│ 1──*│ VirtualLabSubmission │
└──────────┘     └──────────────────────┘
(one per Lab module)

Also: ChatMessage (SenderId, ReceiverId → Users), QuizAttempt (QuizId, StudentId).
```

---

## 6. Controllers & Routes

| Controller | Key routes | Role |
|---|---|---|
| **HomeController** | `/` (landing or role dashboard), `/Home/Search`, `/Home/Privacy`, `/Home/Error` | Public / all |
| **AuthController** | `/Auth/Register`, `/Auth/Login`, `/Auth/Logout`, `/Auth/Profile` | Public / self |
| **CourseController** | `/Course` (catalogue), `/Course/Details/{id}`, `Manage`, `Create`, `Edit`, `SetStatus`, `Delete`, `EditModules`, `AddModule`, `DeleteModule`, `UploadMaterial`, `Enrol` | Public + Educator/Admin |
| **MaterialController** | material file handling | Educator/Admin |
| **VirtualLabController** | `/VirtualLab/Create`, `Edit`, `Delete`, `Workspace/{id}`, `Save` | Educator authors, Student works |
| **AssignmentController** | `/Assignment` list, `Create`, `Submit`, `Grade` | Educator + Student |
| **QuizController** | `/Quiz/Create`, `EditQuestions`, `AddQuestion`, `Take`, `Result` | Educator + Student |
| **DiscussionController** | `/Discussion` list, `Thread`, post, reply, delete | All enrolled |
| **AnnouncementController** | `/Announcement/Index`, `Create`, `Delete` | Educator/Admin post, Student read |
| **ProgressController** | `/Progress` (constellation), `MarkComplete` | Student |
| **CertificateController** | `/Certificate` (list), `/Certificate/View/{id}` (printable) | Student / owner |
| **AdminController** | `/Admin`, `/Admin/Users`, `/Admin/Courses`, `SuspendUser` | Admin |

Default route: `{controller=Home}/{action=Index}/{id?}`.

---

## 7. Views (Pages)

```
Home/        Index (landing), Dashboard (role-aware), Search, Privacy
Auth/        Register, Login, Profile
Course/      Index (catalogue), Details, Manage, Create, Edit, EditModules
VirtualLab/  Create (author/edit), Workspace (student editor + live preview)
Assignment/  Index, Create, Submit, Grade
Quiz/        Index, Create, EditQuestions, Take, Result
Discussion/  Index, Thread
Announcement/Index
Progress/    Index (constellation)
Certificate/ Index, View (printable)
Admin/       Index, Users, Courses
Shared/      _Layout, _Navbar, _Footer, Error, _ValidationScriptsPartial
```

---

## 8. Design System (FRAME.md)

A high-contrast editorial poster aesthetic, enforced everywhere.

- **Colours (four only):** `#FFFFFF` white, `#1C1410` dark, `#D8000F` red, `#F5F2EF` cream. No other colours, no gradients.
- **Fonts (three only):** `Shrikhand` (display/headers), `Libre Baskerville` (body), `Space Grotesk` (labels/buttons/nav).
- **Shape:** square corners everywhere; the only `4px` radius is the `.pf-status` pill.
- **Shadows:** none (`box-shadow` is never used; hover "lift" uses `translateY`). Only stacked `text-shadow` on red display text.
- **Signature borders:** 4px red left-bar cards, 3px dark outer form panels, double-border data tables, 3px red navbar underline.
- **Key classes:** `.pf-btn`, `.pf-input`, `.pf-card`, `.pf-table`, `.pf-progress`, `.pf-stat-grid`, `.pf-alert`, `.pf-status`, `.pf-footer`, `.pf-hero`, `.pf-feature`, `.pf-cert`.

---

## 9. Security

- **Passwords**: BCrypt hashes only (`$2a$...`); never stored or logged in plain text.
- **Sessions**: `HttpOnly`, essential cookie, 2-hour idle timeout (`Program.cs`).
- **Authorization**: every protected action calls `SessionHelper.RequireRole`; educators can only manage their own courses (`CanManageCourse`); suspended users (`IsActive = false`) cannot log in.
- **CSRF**: every POST form carries `@Html.AntiForgeryToken()` and every POST action has `[ValidateAntiForgeryToken]`.
- **SQL injection**: all data access is EF Core LINQ (parameterized); no raw SQL concatenation.
- **Virtual Lab isolation**: student code runs in a sandboxed iframe via `srcdoc` with `sandbox="allow-scripts"` — no same-origin access, no server execution.
- **Input validation**: DataAnnotations on view models + `ModelState.IsValid` on the server, plus client-side jQuery validation.

---

## 10. Seeded Demo Data & Test Accounts

The database auto-seeds on first run (idempotent — only if `Users` is empty).
All passwords follow the pattern below.

| Role | Email | Password |
|---|---|---|
| Admin | `sushanka@pathforge.test` | `Admin@123` |
| Educator | `maya@pathforge.test` | `Educator@123` |
| Educator | `samuel@pathforge.test` | `Educator@123` |
| Educator | `tara@pathforge.test` | `Educator@123` |
| Educator | `jon@pathforge.test` | `Educator@123` |
| Student | `priya@pathforge.test` | `Student@123` |
| Student | `liam@pathforge.test` | `Student@123` |
| Student | `noah@pathforge.test` | `Student@123` |
| Student | `amara@pathforge.test` | `Student@123` |
| Student | `wei@pathforge.test` | `Student@123` |
| Student | `fatima@pathforge.test` | `Student@123` |
| Student | `diego@pathforge.test` | `Student@123` |

**Seeded content:** 6 courses (4 Live, 1 Draft, 1 Archived) across Web
Development, Data & Backend and Design; 14 modules including **2 Virtual
Labs**; quizzes with questions and options; assignments with submitted,
graded and late examples; discussion threads with replies; pinned and normal
announcements; enrolments at varying completion; quiz attempts; and **one
earned certificate** (Wei → UI Design Fundamentals).

> `wei@pathforge.test` is the best account to demo certificates; any educator
> account demos authoring; `sushanka@pathforge.test` is the administrator.

### Homepage vs Dashboard
The marketing **homepage is always at `/`** (shown to everyone, logged in or
not). Logged-in users reach their **role-aware dashboard** at `/Home/Dashboard`
(also linked in the navbar and via the hero "Go to Dashboard" button). Login
redirects students/educators to the dashboard and admins to `/Admin`.

### Password strength
Registration and password changes require a **strong password**: at least 8
characters with an uppercase letter, a lowercase letter, a number, and a
special character (validated client- and server-side). All seeded demo
passwords meet this rule.

### Confirmation on sensitive actions
Destructive or high-impact actions prompt for confirmation before running:
deleting a course/module/lab/announcement/post, suspending or reactivating a
user, publishing/unpublishing a course, and changing your password.

---

## 11. Project Structure

```text
PathForge/
├── Controllers/        12 MVC controllers (see §6)
├── Models/
│   ├── Entities/       18 EF Core entities (see §4)
│   └── ViewModels/     Login, Register, Course, Assignment, Quiz,
│                       Profile, VirtualLab, Dashboard view models
├── Data/
│   ├── PathForgeDbContext.cs   DbSets + Fluent API (cascade rules, indexes)
│   └── DataSeeder.cs           Idempotent demo-data seeder (runs on startup)
├── Migrations/         EF Core SQL Server migration + model snapshot
├── Views/              Razor views grouped by controller (see §7)
├── wwwroot/
│   ├── css/            site.css (design system), constellation.css
│   ├── js/             site.js, constellation.js
│   └── uploads/        materials/ and submissions/ (git-ignored)
├── Helpers/            SessionHelper.cs (auth/role checks)
├── appsettings.json    SQL Server connection string + logging
├── Program.cs          Middleware, session, DbContext, startup migrate+seed
├── README.md           This document
└── SETUP.md            How to run + database start/stop + troubleshooting
```

---

## 12. Assignment Requirement Mapping

| Brief requirement | Where it is implemented |
|---|---|
| Interlinked pages, navigation | Navbar + footer on every page, breadcrumbs, back links |
| HTML5 semantic elements | `<nav> <main> <section> <article> <form> <label>` in views |
| External / internal / inline CSS | `site.css`; `<style>` blocks in Progress & Lab views; inline `style=` attributes |
| DB INSERT / SELECT / UPDATE / DELETE | Register & Create (insert), catalogue & admin tables (select), Edit & Grade (update), Delete course/module/post (delete) |
| Registration + member area + admin area | `/Auth/Register`, login-gated student pages, `/Admin` |
| Form validation (client + server) | jQuery validation + DataAnnotations/`ModelState` |
| Client-side & server-side scripting | `site.js` / controllers |
| Multimedia | Course thumbnails, video materials, embedded links |
| Creative feature | **Virtual Lab** (live coding sandbox) + **Constellation** star map |
| Database connectivity | EF Core → SQL Server Express (`.\SQLEXPRESS01`) |

For running instructions and the database start/stop behaviour across
restarts, see **[SETUP.md](SETUP.md)**.
