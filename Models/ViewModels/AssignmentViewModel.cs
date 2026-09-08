using System.ComponentModel.DataAnnotations;

namespace PathForge.Models.ViewModels
{
    public class AssignmentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Due date is required")]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(7);

        public int MaxMarks { get; set; } = 100;

        public int CourseId { get; set; }
        public string? CourseTitle { get; set; }

        public bool HasSubmitted { get; set; }
        public string? SubmissionStatus { get; set; }
        public int? MarksAwarded { get; set; }
    }
}
