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
