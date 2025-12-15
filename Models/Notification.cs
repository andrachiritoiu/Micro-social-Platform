using System.ComponentModel.DataAnnotations;

namespace MicroSocialPlatform.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        // Cărui utilizator îi aparține notificarea
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // Tipul notificării: FollowRequest, FollowAccepted, NewMessage, NewComment, NewReaction, GroupInvite
        [Required]
        public string Type { get; set; }

        // Titlul notificării
        [Required]
        public string Title { get; set; }

        // Conținutul notificării
        public string? Content { get; set; }

        // Link către resursa relevantă (ex: /Users/Show/{id}, /Groups/Details/{id})
        public string? Link { get; set; }

        // ID-ul utilizatorului care a generat notificarea (pentru FollowRequest, etc.)
        public string? RelatedUserId { get; set; }
        public virtual ApplicationUser? RelatedUser { get; set; }

        // Dacă notificarea a fost citită
        public bool IsRead { get; set; } = false;

        // Data creării
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

