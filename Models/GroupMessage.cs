using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class GroupMessage
    {
        [Key]
        public int Id { get; set; }

        //FK
        public int GroupId { get; set; }
        public virtual Group Group { get; set; }

        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }



        [Required(ErrorMessage = "The message cannot be empty.")] 
        public string Content { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
