using Microsoft.EntityFrameworkCore;
using PathForge.Models.Entities;

namespace PathForge.Data
{
    /// <summary>
    /// Seeds the database with realistic demo data the first time it runs.
    /// Idempotent: if any users already exist, it does nothing.
    /// </summary>
    public static class DataSeeder
    {
        public static async Task SeedAsync(PathForgeDbContext db)
        {
            // Make sure the schema exists, then bail out if already seeded.
            await db.Database.MigrateAsync();
            if (await db.Users.AnyAsync())
                return;

            string Hash(string pw) => BCrypt.Net.BCrypt.HashPassword(pw);
            var now = DateTime.Now;

            // ── USERS ────────────────────────────────────────────────
            var admin = new User
            {
                FullName = "Alex Rivera",
                Email = "admin@pathforge.test",
                Role = "Admin",
                PasswordHash = Hash("Admin@123"),
                Bio = "Platform administrator for PathForge.",
                CreatedAt = now.AddDays(-60)
            };

            var eduMaya = new User
            {
                FullName = "Maya Chen",
                Email = "maya@pathforge.test",
                Role = "Educator",
                PasswordHash = Hash("Educator@123"),
                Bio = "Front-end engineer turned educator. Loves teaching the web platform.",
                CreatedAt = now.AddDays(-50)
            };

            var eduSam = new User
            {
                FullName = "Samuel Okoye",
                Email = "samuel@pathforge.test",
                Role = "Educator",
                PasswordHash = Hash("Educator@123"),
                Bio = "Backend and databases specialist with 10 years in industry.",
                CreatedAt = now.AddDays(-48)
            };

            var stuPriya = new User
            {
                FullName = "Priya Nair",
                Email = "priya@pathforge.test",
                Role = "Student",
                PasswordHash = Hash("Student@123"),
                Bio = "CS undergrad focused on web development.",
                CreatedAt = now.AddDays(-30)
            };

            var stuLiam = new User
            {
                FullName = "Liam O'Brien",
                Email = "liam@pathforge.test",
                Role = "Student",
                PasswordHash = Hash("Student@123"),
                Bio = "Career switcher learning to code.",
                CreatedAt = now.AddDays(-25)
            };

            var stuNoah = new User
            {
                FullName = "Noah Kim",
                Email = "noah@pathforge.test",
                Role = "Student",
                PasswordHash = Hash("Student@123"),
                Bio = "Bootcamp graduate sharpening fundamentals.",
                CreatedAt = now.AddDays(-20)
            };

            db.Users.AddRange(admin, eduMaya, eduSam, stuPriya, stuLiam, stuNoah);
            await db.SaveChangesAsync();

            // ── COURSE 1: Web Foundations (Maya, Live) ───────────────
            var webCourse = new Course
            {
                Title = "Web Foundations: HTML, CSS & JavaScript",
                Description = "Start from zero and build real, standards-based web pages. " +
                              "Covers semantic HTML, modern CSS layout, and interactive JavaScript.",
                Category = "Web Development",
                Status = "Live",
                EducatorId = eduMaya.Id,
                CreatedAt = now.AddDays(-45)
            };

            webCourse.Modules = new List<Module>
            {
                new Module { Title = "Semantic HTML", Type = "Lesson", DisplayOrder = 1,
                    Description = "Document structure, landmarks and accessible markup.",
                    Materials = new List<Material>
                    {
                        new Material { Title = "HTML Cheat Sheet", Type = "Document",
                            FileUrl = "/uploads/materials/html-cheatsheet.pdf", FileSizeBytes = 240000 },
                        new Material { Title = "MDN: HTML elements", Type = "Link",
                            FileUrl = "https://developer.mozilla.org/en-US/docs/Web/HTML/Element" }
                    } },
                new Module { Title = "CSS Layout with Flexbox & Grid", Type = "Lesson", DisplayOrder = 2,
                    Description = "Build responsive layouts without hacks.",
                    Materials = new List<Material>
                    {
                        new Material { Title = "Flexbox Slides", Type = "Slide",
                            FileUrl = "/uploads/materials/flexbox-slides.pdf", FileSizeBytes = 980000 }
                    } },
                new Module { Title = "JavaScript Basics", Type = "Video", DisplayOrder = 3,
                    Description = "Variables, functions, and the DOM.",
                    Materials = new List<Material>
                    {
                        new Material { Title = "Intro to JS (video)", Type = "Video",
                            FileUrl = "https://www.youtube.com/watch?v=W6NZfCO5SIk" }
                    } },
                new Module { Title = "Mini Project: Build a Landing Page", Type = "Lab", DisplayOrder = 4,
                    Description = "Apply everything in a hands-on build." }
            };

            webCourse.Quizzes = new List<Quiz>
            {
                new Quiz
                {
                    Title = "HTML & CSS Checkpoint",
                    Description = "Quick check on the first two modules.",
                    TimeLimitMinutes = 15,
                    PassMarkPercent = 60,
                    CreatedAt = now.AddDays(-40),
                    Questions = new List<QuizQuestion>
                    {
                        new QuizQuestion { QuestionText = "Which tag defines the main content landmark?",
                            Marks = 1, DisplayOrder = 1, Options = new List<QuizOption>
                            {
                                new QuizOption { OptionText = "<main>", IsCorrect = true },
                                new QuizOption { OptionText = "<section>", IsCorrect = false },
                                new QuizOption { OptionText = "<div id=\"main\">", IsCorrect = false },
                                new QuizOption { OptionText = "<content>", IsCorrect = false }
                            } },
                        new QuizQuestion { QuestionText = "Which CSS property creates a flex container?",
                            Marks = 1, DisplayOrder = 2, Options = new List<QuizOption>
                            {
                                new QuizOption { OptionText = "display: flex", IsCorrect = true },
                                new QuizOption { OptionText = "position: flex", IsCorrect = false },
                                new QuizOption { OptionText = "float: flex", IsCorrect = false },
                                new QuizOption { OptionText = "layout: flex", IsCorrect = false }
                            } },
                        new QuizQuestion { QuestionText = "What does the 'C' in CSS stand for?",
                            Marks = 1, DisplayOrder = 3, Options = new List<QuizOption>
                            {
                                new QuizOption { OptionText = "Cascading", IsCorrect = true },
                                new QuizOption { OptionText = "Computed", IsCorrect = false },
                                new QuizOption { OptionText = "Central", IsCorrect = false },
                                new QuizOption { OptionText = "Custom", IsCorrect = false }
                            } }
                    }
                }
            };

            webCourse.Assignments = new List<Assignment>
            {
                new Assignment
                {
                    Title = "Build a Personal Profile Page",
                    Description = "Submit an HTML/CSS page with a header, about section, and contact form.",
                    DueDate = now.AddDays(7),
                    MaxMarks = 100,
                    CreatedAt = now.AddDays(-38)
                }
            };

            // ── COURSE 2: Databases (Samuel, Live) ───────────────────
            var dbCourse = new Course
            {
                Title = "Relational Databases & SQL",
                Description = "Design normalized schemas and write effective SQL queries. " +
                              "From primary keys to joins, indexes and transactions.",
                Category = "Data & Backend",
                Status = "Live",
                EducatorId = eduSam.Id,
                CreatedAt = now.AddDays(-35)
            };

            dbCourse.Modules = new List<Module>
            {
                new Module { Title = "Entities & Relationships", Type = "Reading", DisplayOrder = 1,
                    Description = "ERDs, keys and cardinality." },
                new Module { Title = "Writing SELECT Queries", Type = "Lesson", DisplayOrder = 2,
                    Description = "Filtering, sorting and aggregation." },
                new Module { Title = "Joins Deep Dive", Type = "Lesson", DisplayOrder = 3,
                    Description = "Inner, left, right and self joins." }
            };

            dbCourse.Assignments = new List<Assignment>
            {
                new Assignment
                {
                    Title = "Design a Library Schema",
                    Description = "Produce an ERD and the CREATE TABLE statements for a small library system.",
                    DueDate = now.AddDays(-2), // past due → lets you demo 'Late'
                    MaxMarks = 50,
                    CreatedAt = now.AddDays(-30)
                }
            };

            // ── COURSE 3: Draft course (Maya) ────────────────────────
            var draftCourse = new Course
            {
                Title = "Intro to Accessibility (Coming Soon)",
                Description = "A work-in-progress course on building inclusive web experiences.",
                Category = "Web Development",
                Status = "Draft",
                EducatorId = eduMaya.Id,
                CreatedAt = now.AddDays(-5)
            };

            db.Courses.AddRange(webCourse, dbCourse, draftCourse);
            await db.SaveChangesAsync();

            // ── ENROLMENTS + PROGRESS ────────────────────────────────
            // Priya: enrolled in Web Foundations, first module complete.
            await EnrolWithProgressAsync(db, stuPriya.Id, webCourse.Id, completedModules: 1);
            // Liam: enrolled in Web Foundations, two modules complete.
            await EnrolWithProgressAsync(db, stuLiam.Id, webCourse.Id, completedModules: 2);
            // Priya: also enrolled in Databases, nothing completed yet.
            await EnrolWithProgressAsync(db, stuPriya.Id, dbCourse.Id, completedModules: 0);
            // Noah: enrolled in Databases.
            await EnrolWithProgressAsync(db, stuNoah.Id, dbCourse.Id, completedModules: 0);

            // ── A QUIZ ATTEMPT (Liam passed the Web checkpoint) ──────
            var webQuiz = webCourse.Quizzes.First();
            db.QuizAttempts.Add(new QuizAttempt
            {
                QuizId = webQuiz.Id,
                StudentId = stuLiam.Id,
                CorrectAnswers = 3,
                TotalQuestions = 3,
                ScorePercent = 100,
                Passed = true,
                AttemptedAt = now.AddDays(-10)
            });

            // ── AN ASSIGNMENT SUBMISSION (Priya submitted, ungraded) ─
            var webAssignment = webCourse.Assignments.First();
            db.AssignmentSubmissions.Add(new AssignmentSubmission
            {
                AssignmentId = webAssignment.Id,
                StudentId = stuPriya.Id,
                Comment = "Here is my profile page. Open index.html in a browser.",
                Status = "Submitted",
                SubmittedAt = now.AddDays(-1)
            });

            // ── DISCUSSION THREAD + REPLY (Web Foundations) ──────────
            var thread = new DiscussionPost
            {
                CourseId = webCourse.Id,
                AuthorId = stuPriya.Id,
                Title = "Flexbox vs Grid — when to use which?",
                Body = "I keep reaching for Flexbox for everything. When is Grid the better choice?",
                ParentPostId = null,
                CreatedAt = now.AddDays(-8)
            };
            db.DiscussionPosts.Add(thread);
            await db.SaveChangesAsync();

            db.DiscussionPosts.Add(new DiscussionPost
            {
                CourseId = webCourse.Id,
                AuthorId = eduMaya.Id,
                Title = "Re: thread",
                Body = "Great question! Use Grid for two-dimensional layouts (rows AND columns) " +
                       "and Flexbox for one-dimensional flows. They work well together.",
                ParentPostId = thread.Id,
                CreatedAt = now.AddDays(-7)
            });

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Enrols a student and creates per-module progress rows.
        /// The first `completedModules` are Completed, the next one is InProgress,
        /// and the rest are Locked — matching the app's unlock logic.
        /// </summary>
        private static async Task EnrolWithProgressAsync(
            PathForgeDbContext db, int studentId, int courseId, int completedModules)
        {
            var now = DateTime.Now;

            db.CourseEnrolments.Add(new CourseEnrolment
            {
                StudentId = studentId,
                CourseId = courseId,
                EnrolledAt = now.AddDays(-15)
            });

            var modules = await db.Modules
                .Where(m => m.CourseId == courseId)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            for (int i = 0; i < modules.Count; i++)
            {
                string status;
                if (i < completedModules) status = "Completed";
                else if (i == completedModules) status = "InProgress";
                else status = "Locked";

                db.StudentProgress.Add(new StudentProgress
                {
                    StudentId = studentId,
                    ModuleId = modules[i].Id,
                    Status = status,
                    StartedAt = status == "Locked" ? null : now.AddDays(-14 + i),
                    CompletedAt = status == "Completed" ? now.AddDays(-12 + i) : null,
                    UpdatedAt = now.AddDays(-12 + i)
                });
            }

            await db.SaveChangesAsync();
        }
    }
}
