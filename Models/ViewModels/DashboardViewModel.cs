using PathForge.Models.Entities;

namespace PathForge.Models.ViewModels
{
    // Role-aware home dashboard shown to logged-in users.
    public class DashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        // ── Student ──
        public List<CourseProgressRow> InProgressCourses { get; set; } = new();
        public List<Assignment> UpcomingAssignments { get; set; } = new();
        public int LabsPassed { get; set; }
        public int CertificatesEarned { get; set; }
        public List<Announcement> RecentAnnouncements { get; set; } = new();

        // ── Educator ──
        public List<Course> MyCourses { get; set; } = new();
        public int UngradedSubmissions { get; set; }
        public int TotalStudents { get; set; }

        // ── Shared stats ──
        public int StatA { get; set; }
        public int StatB { get; set; }
        public int StatC { get; set; }
        public string StatALabel { get; set; } = "";
        public string StatBLabel { get; set; } = "";
        public string StatCLabel { get; set; } = "";
    }

    public class CourseProgressRow
    {
        public Course Course { get; set; } = null!;
        public int TotalModules { get; set; }
        public int CompletedModules { get; set; }
        public int Percent => TotalModules == 0 ? 0 : (int)Math.Round(100.0 * CompletedModules / TotalModules);
    }

    // Global search results.
    public class SearchResultViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<Course> Courses { get; set; } = new();
        public List<DiscussionPost> Discussions { get; set; } = new();
    }
}
