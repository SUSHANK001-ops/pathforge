using System.ComponentModel.DataAnnotations;
using PathForge.Models.Entities;

namespace PathForge.Models.ViewModels
{
    public class QuizViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
        public int TimeLimitMinutes { get; set; } = 30;
        public int PassMarkPercent { get; set; } = 50;

        public int CourseId { get; set; }
        public string? CourseTitle { get; set; }

        public int QuestionCount { get; set; }
        public List<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    }
}
