using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }


        //FK
        public string UserId { get; set; }

        //Descrierea
        public string? Content { get; set; }


        //Fisierul Media - poate face postare si fara media
        public string? MediaUrl { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }


        //Relatii (Navigation Properties)
        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }
        public virtual ICollection<Reaction> Reactions { get; set; }

    }
}
