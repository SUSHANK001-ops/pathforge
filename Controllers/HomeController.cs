using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PathForge.Data;
using PathForge.Helpers;
using PathForge.Models;
using PathForge.Models.Entities;
using PathForge.Models.ViewModels;

namespace PathForge.Controllers
{
    public class HomeController : Controller
    {
        private readonly PathForgeDbContext _db;

        public HomeController(PathForgeDbContext db)
        {
            _db = db;
        }

        // Public marketing landing page. Logged-in users also see it,
        // but with the hero buttons pointing at their dashboard.
        public IActionResult Index()
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "";
            ViewBag.IsLoggedIn = !string.IsNullOrEmpty(role);
            ViewBag.UserRole = role;
            return View();
        }

        // Role-aware dashboard for logged-in users.
        public async Task<IActionResult> Dashboard()
        {
            if (SessionHelper.RequireLogin(HttpContext, TempData))
                return RedirectToAction("Login", "Auth");

            return View(await BuildDashboardAsync());
        }

        private async Task<DashboardViewModel> BuildDashboardAsync()
        {
            int userId  = SessionHelper.GetUserId(HttpContext);
            string role = SessionHelper.GetUserRole(HttpContext);

            var vm = new DashboardViewModel
            {
                UserName = HttpContext.Session.GetString("UserName") ?? "",
                Role     = role
            };

            if (role == "Student")
            {
                var enrolments = await _db.CourseEnrolments
                    .Include(e => e.Course).ThenInclude(c => c.Modules)
                    .Include(e => e.Course).ThenInclude(c => c.Educator)
                    .Where(e => e.StudentId == userId)
                    .ToListAsync();

                var progress = await _db.StudentProgress
                    .Where(p => p.StudentId == userId)
                    .ToListAsync();

                foreach (var e in enrolments)
                {
                    var moduleIds = e.Course.Modules.Select(m => m.Id).ToHashSet();
                    int total     = moduleIds.Count;
                    int completed = progress.Count(p => moduleIds.Contains(p.ModuleId) && p.Status == "Completed");
                    vm.InProgressCourses.Add(new CourseProgressRow
                    {
                        Course = e.Course,
                        TotalModules = total,
                        CompletedModules = completed
                    });
                }

                var enrolledCourseIds = enrolments.Select(e => e.CourseId).ToList();
                vm.UpcomingAssignments = await _db.Assignments
                    .Include(a => a.Course)
                    .Where(a => enrolledCourseIds.Contains(a.CourseId) && a.DueDate >= DateTime.Now)
                    .OrderBy(a => a.DueDate)
                    .Take(5)
                    .ToListAsync();

                vm.LabsPassed = await _db.VirtualLabSubmissions
                    .CountAsync(s => s.StudentId == userId && s.Status == "Passed");

                vm.CertificatesEarned = await _db.Certificates
                    .CountAsync(c => c.StudentId == userId);

                vm.RecentAnnouncements = await _db.Announcements
                    .Include(a => a.Course)
                    .Include(a => a.Author)
                    .Where(a => enrolledCourseIds.Contains(a.CourseId))
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(4)
                    .ToListAsync();

                vm.StatA = enrolments.Count;            vm.StatALabel = "Enrolled Courses";
                vm.StatB = vm.InProgressCourses.Count(c => c.Percent == 100); vm.StatBLabel = "Completed";
                vm.StatC = vm.CertificatesEarned;       vm.StatCLabel = "Certificates";
            }
            else if (role == "Educator" || role == "Admin")
            {
                var query = _db.Courses
                    .Include(c => c.Enrolments)
                    .Include(c => c.Assignments).ThenInclude(a => a.Submissions)
                    .AsQueryable();

                if (role == "Educator")
                    query = query.Where(c => c.EducatorId == userId);

                vm.MyCourses = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

                vm.UngradedSubmissions = vm.MyCourses
                    .SelectMany(c => c.Assignments)
                    .SelectMany(a => a.Submissions)
                    .Count(s => s.Status == "Submitted" || s.Status == "Late");

                vm.TotalStudents = vm.MyCourses
                    .SelectMany(c => c.Enrolments)
                    .Select(e => e.StudentId)
                    .Distinct()
                    .Count();

                vm.StatA = vm.MyCourses.Count;                               vm.StatALabel = "Courses";
                vm.StatB = vm.TotalStudents;                                 vm.StatBLabel = "Students";
                vm.StatC = vm.UngradedSubmissions;                           vm.StatCLabel = "To Grade";
            }

            return vm;
        }

        // ── GET /Home/Search?q=... — Global search ──────────────────
        public async Task<IActionResult> Search(string? q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return RedirectToAction("Index", "Course");

            var vm = new SearchResultViewModel { Query = q.Trim() };

            vm.Courses = await _db.Courses
                .Include(c => c.Educator)
                .Where(c => c.Status == "Live" &&
                            (c.Title.Contains(vm.Query) || c.Description.Contains(vm.Query)))
                .Take(20)
                .ToListAsync();

            vm.Discussions = await _db.DiscussionPosts
                .Include(p => p.Course)
                .Include(p => p.Author)
                .Where(p => p.ParentPostId == null &&
                            (p.Title.Contains(vm.Query) || p.Body.Contains(vm.Query)))
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
