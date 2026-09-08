using System.ComponentModel.DataAnnotations;
using PathForge.Models.Entities;

namespace PathForge.Models.ViewModels
{
    public class CourseViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        public string Status { get; set; } = "Draft";
        public string? ThumbnailUrl { get; set; }

        public int EducatorId { get; set; }
        public string? EducatorName { get; set; }

        public int EnrolledCount { get; set; }
        public int ModuleCount { get; set; }
        public List<Module> Modules { get; set; } = new List<Module>();
    }
}
