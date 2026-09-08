using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class Module
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Type: "Lesson" | "Video" | "Lab" | "Reading"
        [MaxLength(30)]
        public string Type { get; set; } = "Lesson";

        // Order controls the constellation sequence
        public int DisplayOrder { get; set; }

        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;

        public ICollection<Material> Materials { get; set; } = new List<Material>();
        public ICollection<StudentProgress> ProgressRecords { get; set; } = new List<StudentProgress>();
    }
}
