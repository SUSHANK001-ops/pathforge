using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class StudentProgress
    {
        public int Id { get; set; }

        // Status: "Locked" | "InProgress" | "Completed"
        [MaxLength(20)]
        public string Status { get; set; } = "Locked";

        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public User Student { get; set; } = null!;

        public int ModuleId { get; set; }
        [ForeignKey("ModuleId")]
        public Module Module { get; set; } = null!;
    }
}
