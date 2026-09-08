using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathForge.Models.Entities
{
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        public string MessageText { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.Now;

        public int SenderId { get; set; }
        [ForeignKey("SenderId")]
        public User Sender { get; set; } = null!;

        public int ReceiverId { get; set; }
        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; } = null!;
    }
}
