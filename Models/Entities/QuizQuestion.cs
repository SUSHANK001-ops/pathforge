using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class QuizQuestion
    {
        public int Id { get; set; }

        [Required]
        public string QuestionText { get; set; } = string.Empty;

        public int Marks { get; set; } = 1;
        public int DisplayOrder { get; set; }

        public int QuizId { get; set; }
        [ForeignKey("QuizId")]
        public Quiz Quiz { get; set; } = null!;

        public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
    }
}
