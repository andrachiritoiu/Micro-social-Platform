using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MicroSocialPlatform.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(50)]
        public string FirstName { get; set; }


        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(50)]
        public string LastName { get; set; }


        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")] public string? Description { get; set; }


        //este obligatoarie
        public string ProfileImage { get; set; }


        public bool IsPrivate { get; set; } = false;


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        //Proprietate ajutatoare care nu se salveaza in Bd
        [NotMapped]
        public string FullName
        {
            get { return $"{FirstName} {LastName}"; }
        }



        //Relatii (Navigation Properties)
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<Comment> Comments { get; set;} = new List<Comment>();
        public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

        //M:M
        public virtual ICollection<Follow> Followers { get; set; } = new List<Follow>();
        public virtual ICollection<Follow> Following { get; set; } = new List<Follow>();


        public virtual ICollection<GroupMembership> GroupMemberships { get; set; } = new List<GroupMembership>();


    }
}
