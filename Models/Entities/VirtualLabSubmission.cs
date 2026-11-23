using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    /// <summary>
    /// A student's saved work for a virtual lab, plus the result of the
    /// automated marker check.
    /// </summary>
    public class VirtualLabSubmission
    {
        public int Id { get; set; }

        public string Html { get; set; } = string.Empty;
        public string Css  { get; set; } = string.Empty;
        public string Js   { get; set; } = string.Empty;

        // Status: "InProgress" | "Passed"
        [MaxLength(20)]
        public string Status { get; set; } = "InProgress";

        // How many expected markers were satisfied at the last check.
        public int ChecksPassed { get; set; }
        public int ChecksTotal  { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime? PassedAt { get; set; }

        public int VirtualLabId { get; set; }
        [ForeignKey("VirtualLabId")]
        public VirtualLab VirtualLab { get; set; } = null!;

        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public User Student { get; set; } = null!;
    }
}
