using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class DiscussionPost
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsFlagged { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Null = root post, not null = reply to another post
        public int? ParentPostId { get; set; }
        [ForeignKey("ParentPostId")]
        public DiscussionPost? ParentPost { get; set; }

        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;

        public int AuthorId { get; set; }
        [ForeignKey("AuthorId")]
        public User Author { get; set; } = null!;

        public ICollection<DiscussionPost> Replies { get; set; } = new List<DiscussionPost>();
    }
}
