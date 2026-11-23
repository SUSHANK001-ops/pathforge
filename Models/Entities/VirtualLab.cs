using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    /// <summary>
    /// A browser-based coding sandbox attached to a module.
    /// The student edits HTML/CSS/JS and sees a live preview; code runs
    /// entirely client-side in a sandboxed iframe, so nothing executes on the server.
    /// </summary>
    public class VirtualLab
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        // Rich instructions / brief shown beside the editor.
        [Required]
        public string Instructions { get; set; } = string.Empty;

        // Starter code the student begins from.
        public string StarterHtml { get; set; } = string.Empty;
        public string StarterCss  { get; set; } = string.Empty;
        public string StarterJs   { get; set; } = string.Empty;

        // Newline-separated, case-insensitive substrings the student's combined
        // code must contain to pass the automated check (e.g. "<h1", "flex").
        public string? ExpectedMarkers { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // One lab per module.
        public int ModuleId { get; set; }
        [ForeignKey("ModuleId")]
        public Module Module { get; set; } = null!;

        public ICollection<VirtualLabSubmission> Submissions { get; set; } = new List<VirtualLabSubmission>();
    }
}
