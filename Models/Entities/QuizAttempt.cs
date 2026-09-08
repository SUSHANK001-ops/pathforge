using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class QuizAttempt
    {
        public int Id { get; set; }
        public int ScorePercent { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public bool Passed { get; set; }
        public DateTime AttemptedAt { get; set; } = DateTime.Now;

        public int QuizId { get; set; }
        [ForeignKey("QuizId")]
        public Quiz Quiz { get; set; } = null!;

        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public User Student { get; set; } = null!;
    }
}
