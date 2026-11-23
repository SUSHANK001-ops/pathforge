using System.ComponentModel.DataAnnotations;

namespace PathForge.Models.ViewModels
{
    // Educator form: create or edit a lab for a module.
    public class VirtualLabViewModel
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Lab title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Instructions are required")]
        public string Instructions { get; set; } = string.Empty;

        public string StarterHtml { get; set; } = string.Empty;
        public string StarterCss  { get; set; } = string.Empty;
        public string StarterJs   { get; set; } = string.Empty;

        [Display(Name = "Expected markers")]
        public string? ExpectedMarkers { get; set; }
    }

    // Student workspace: the editor state when saving / running checks.
    public class VirtualLabWorkViewModel
    {
        public int LabId { get; set; }
        public string Html { get; set; } = string.Empty;
        public string Css  { get; set; } = string.Empty;
        public string Js   { get; set; } = string.Empty;
    }
}
