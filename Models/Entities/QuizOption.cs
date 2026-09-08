using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class QuizOption
    {
        public int Id { get; set; }

        [Required]
        public string OptionText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; } = false;

        public int QuestionId { get; set; }
        [ForeignKey("QuestionId")]
        public QuizQuestion Question { get; set; } = null!;
    }
}
