using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace MicroSocialPlatform.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        // Titlul (Opțional)
        public string? Title { get; set; }

        // Descrierea
        public string? Content { get; set; }

        // Fisierul Media
        public string? MediaUrl { get; set; }

        // Data Creării (setată automat la instanțiere)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Data Ultimei Modificări (Opțional, folosit la Edit)
        public DateTime? UpdatedAt { get; set; }
        // Dacă ai vrut 'DeletedAt' pentru soft delete, ar fi: public DateTime? DeletedAt { get; set; }

        // FK
        [Required]
        public string UserId { get; set; }

        // Relatii (Navigation Properties)
        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
    }
}