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
