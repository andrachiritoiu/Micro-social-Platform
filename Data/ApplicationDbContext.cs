using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Models;

namespace MicroSocialPlatform.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        //Tabelele
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Reaction> Reactions { get; set; }

        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMembership> GroupMemberships { get; set; }
        public DbSet<GroupMessage> GroupMessages { get; set; }

        public DbSet<Follow> Follows { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Message> Messages { get; set; }  //pt chat privat



        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); //pastreza configuratiile implicite


            //Configurare relatii Many-to-many

            //Un user nu poate sa reactioneze de mai multe ori la aceeasi postare
            builder.Entity<Reaction>()
                .HasKey(r => new { r.UserId, r.PostId });

            //Relatia User -> Reaction
            //Daca stergi userul, sterge si reactiile lui
            builder.Entity<Reaction>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reactions)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relatia Post -> Reaction
            //Daca stergi postarea, sterge si reactiile la ea
            builder.Entity<Reaction>()
                .HasOne(r => r.Post)
                .WithMany(p => p.Reactions)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            //Un memebru poate sa fie in acelasi grup o singura data
            builder.Entity<GroupMembership>()
                .HasKey(gm => new { gm.UserId, gm.GroupId });

            // User -> Membership
            //Leaga membrul de utilizator - daca stergi utilizatorul, sterge si proprietatea lui de membru
            builder.Entity<GroupMembership>()
                .HasOne(gm => gm.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Grup -> Membership (Cascade: Stergi grupul -> dispar membrii din tabelul de legatura)
            builder.Entity<GroupMembership>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.GroupMemberships)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> GroupMessage
            //Daca sterg un user, sa nu se stearga mesajele din grupurile in care a fost membru
            builder.Entity<GroupMessage>()
                .HasOne(gm => gm.User)
                .WithMany()
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Grup -> GroupMessage (Cascade: Stergi grupul -> dispar mesajele)
            builder.Entity<GroupMessage>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.GroupPosts) 
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            //Poti sa ai o singura relatie de follow intre doi utilizatori
            builder.Entity<Follow>()
                .HasKey(f => new { f.FollowerId, f.FollowedId });

            //Leaga follower-ul de utilizator - daca stergi utilizatorul, sterge si relatiile lui de follow
            //Follower
            builder.Entity<Follow>()
                .HasOne(f => f.Follower)
                .WithMany(u => u.Following)        //Se leaga de lista Following - are mai multe persoane pe care le urmareste
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            //Followed
            builder.Entity<Follow>()
                .HasOne(f => f.Followed)
                .WithMany(u => u.Followers)        //Se leaga de lista Followers - are mai multi urmaritori
                .HasForeignKey(f => f.FollowedId)
                .OnDelete(DeleteBehavior.Restrict);

            //Daca sterg un user, se sterg psotarile si apoi comentariile(nu direct)
            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            //pt msaje private intre useri
            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict); // nu sterge mesajele automat cand sterg userul

            builder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict); // nu sterge mesajele automat cand sterg userul

        }
    }
}
