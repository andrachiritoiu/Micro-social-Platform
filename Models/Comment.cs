using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }


        //Cine a comentat
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; } //un comentariu apartine unui singur user


        //La ce postare a comentat
        public int PostId { get; set; }
        public virtual Post Post { get; set; }


        [Required(ErrorMessage = "Comentariul nu poate fi gol!")]
        [StringLength(1000, ErrorMessage = "Comentariul nu poate depăși 1000 de caractere.")]
        public string Content { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime Date { get; internal set; }
    }
}
