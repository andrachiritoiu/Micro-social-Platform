using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class Follow
    {
        [Key]
        public int Id { get; set; }

        //Cine initiaza urmariea(Follower)
        public string FollowerId { get; set; }
        public virtual ApplicationUser Follower { get; set; }


        //Pe cine urmareste(Followed)
        public string FollowedId { get; set; }
        public virtual ApplicationUser Followed { get; set; }


        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        public string Status { get; set;  } = "Pending";

    }
}
