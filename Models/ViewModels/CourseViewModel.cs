using System.ComponentModel.DataAnnotations;

namespace PathForge.Models.ViewModels
{
    public class CourseViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        // Thumbnail file (optional upload)
        public IFormFile? ThumbnailFile { get; set; }
        public string? ExistingThumbnailUrl { get; set; }
    }

    public class ModuleViewModel
    {
        public int Id { get; set; }
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Module title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string Type { get; set; } = "Lesson";  // Lesson | Video | Lab | Reading

        public int DisplayOrder { get; set; }
    }

    public class MaterialViewModel
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }

        [Required(ErrorMessage = "Material title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a file type")]
        public string Type { get; set; } = "Document";  // Document | Video | Link | Slide

        // File upload — required only for non-Link types
        public IFormFile? File { get; set; }

        // For links — direct URL
        public string? LinkUrl { get; set; }
    }
}
