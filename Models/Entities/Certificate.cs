using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    /// <summary>
    /// Proof of course completion, issued automatically the first time a
    /// student reaches 100% progress in a course. One per student per course.
    /// </summary>
    public class Certificate
    {
        public int Id { get; set; }

        // A short public reference code shown on the certificate.
        [Required, MaxLength(40)]
        public string SerialCode { get; set; } = string.Empty;

        public DateTime IssuedAt { get; set; } = DateTime.Now;

        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public User Student { get; set; } = null!;

        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;
    }
}
