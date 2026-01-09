using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        // Expeditorul mesajului
        public string SenderId { get; set; }
        public virtual ApplicationUser Sender { get; set; }

        // Destinatarul mesajului
        public string ReceiverId { get; set; }
        public virtual ApplicationUser Receiver { get; set; }

        // Conținutul mesajului
        [Required(ErrorMessage = "The message cannot be empty.")]
        [StringLength(1000, ErrorMessage = "The message cannot exceed 1000 characters.")]
        public string Content { get; set; }

        // Dacă mesajul a fost citit
        public bool IsRead { get; set; } = false;

        // Data creării
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

