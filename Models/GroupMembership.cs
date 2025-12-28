using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class GroupMembership
    {
        public int GroupId { get; set; }
        public virtual Group Group { get; set; }


        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }


        public bool IsAccepted { get; set; } = false;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
