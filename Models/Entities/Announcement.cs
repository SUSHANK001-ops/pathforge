using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    /// <summary>
    /// A course-wide notice posted by an educator (or admin) and shown to
    /// enrolled students on their dashboard and the course page.
    /// </summary>
    public class Announcement
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        // Pinned announcements sort to the top.
        public bool IsPinned { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;

        public int AuthorId { get; set; }
        [ForeignKey("AuthorId")]
        public User Author { get; set; } = null!;
    }
}
