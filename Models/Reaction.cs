using System.ComponentModel.DataAnnotations;


namespace MicroSocialPlatform.Models
{
    public class Reaction
    {
        [Key]
        public int Id { get; set; }

        //Cine a reactionat
        public string UserId { get; set; } 
        public virtual ApplicationUser User { get; set; } //o reactie apartine unui singur user

        //La ce postare a reactionat
        public int PostId { get; set; }
        public virtual Post Post { get; set; }

        //Tipul reactiei: Like, Love, Haha, Wow, Sad, Angry
        [Required]
        public string Type { get; set; }  
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
