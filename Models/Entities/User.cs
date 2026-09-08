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
