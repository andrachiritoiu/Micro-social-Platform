using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        // Cine a comentat
        [Required]
        public string UserId { get; set; } = string.Empty;

        public virtual ApplicationUser? User { get; set; } // un comentariu apartine unui singur user

        // La ce postare a comentat
        [Required]
        public int PostId { get; set; }

        public virtual Post? Post { get; set; }

        // Continut
        [Required(ErrorMessage = "The comment cannot be empty.")]
        [MinLength(2, ErrorMessage = "The comment is too short.")]
        [StringLength(1000, ErrorMessage = "The comment cannot exceed 1000 characters.")]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

       }
}
