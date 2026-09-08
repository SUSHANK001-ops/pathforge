using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class Material
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        // Type: "Document" | "Video" | "Link" | "Slide"
        [MaxLength(30)]
        public string Type { get; set; } = "Document";

        [Required]
        public string FileUrl { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public int ModuleId { get; set; }
        [ForeignKey("ModuleId")]
        public Module Module { get; set; } = null!;
    }
}
