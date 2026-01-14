using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MicroSocialPlatform.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // 1. ASIGURARE ROLURI
                if (!context.Roles.Any())
                {
                    context.Roles.AddRange(
                        new IdentityRole { Id = "role-admin", Name = "Admin", NormalizedName = "ADMIN" },
                        new IdentityRole { Id = "role-user", Name = "User", NormalizedName = "USER" }
                    );
                    context.SaveChanges();
                }

                var hasher = new PasswordHasher<ApplicationUser>();

                // 2. ASIGURARE UTILIZATORI
                // Vom verifica dacă există userii înainte de a-i adăuga pentru a evita duplicatele

                // a) Admin
                if (!context.Users.Any(u => u.Id == "admin-id"))
                {
                    context.Users.Add(new ApplicationUser
                    {
                        Id = "admin-id",
                        UserName = "admin@test.com",
                        Email = "admin@test.com",
                        NormalizedEmail = "ADMIN@TEST.COM",
                        NormalizedUserName = "ADMIN@TEST.COM",
                        FirstName = "Admin",
                        LastName = "System",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(null, "Admin1!"),
                        Description = "System Administrator Account",
                        ProfileImage = "/images/default.png"
                    });
                }

                // b) Diaconescu Maria
                if (!context.Users.Any(u => u.Id == "user-maria-id"))
                {
                    context.Users.Add(new ApplicationUser
                    {
                        Id = "user-maria-id",
                        UserName = "maria.diaconescu@test.com",
                        Email = "maria.diaconescu@test.com",
                        NormalizedEmail = "MARIA.DIACONESCU@TEST.COM",
                        NormalizedUserName = "MARIA.DIACONESCU@TEST.COM",
                        FirstName = "Maria",
                        LastName = "Diaconescu",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(null, "User1!"),
                        Description = "Lover of nature and photography.",
                        ProfileImage = "/images/default.png"
                    });
                }

                // c) Vlad Ioana
                if (!context.Users.Any(u => u.Id == "user-ioana-id"))
                {
                    context.Users.Add(new ApplicationUser
                    {
                        Id = "user-ioana-id",
                        UserName = "ioana.vlad@test.com",
                        Email = "ioana.vlad@test.com",
                        NormalizedEmail = "IOANA.VLAD@TEST.COM",
                        NormalizedUserName = "IOANA.VLAD@TEST.COM",
                        FirstName = "Ioana",
                        LastName = "Vlad",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(null, "User1!"),
                        Description = "Tech enthusiast and coffee addict.",
                        ProfileImage = "/images/default.png"
                    });
                }

                // d) Chipirnic David
                if (!context.Users.Any(u => u.Id == "user-david-id"))
                {
                    context.Users.Add(new ApplicationUser
                    {
                        Id = "user-david-id",
                        UserName = "david.chipirnic@test.com",
                        Email = "david.chipirnic@test.com",
                        NormalizedEmail = "DAVID.CHIPIRNIC@TEST.COM",
                        NormalizedUserName = "DAVID.CHIPIRNIC@TEST.COM",
                        FirstName = "David",
                        LastName = "Chipirnic",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(null, "User1!"),
                        Description = "Always traveling. Catch me if you can.",
                        ProfileImage = "/images/default.png"
                    });
                }

                context.SaveChanges();

                // 3. ASOCIERI ROLURI (Direct pe tabela UserRoles dacă nu există deja)
                if (!context.UserRoles.Any())
                {
                    context.UserRoles.AddRange(
                        new IdentityUserRole<string> { UserId = "admin-id", RoleId = "role-admin" },
                        new IdentityUserRole<string> { UserId = "user-maria-id", RoleId = "role-user" },
                        new IdentityUserRole<string> { UserId = "user-ioana-id", RoleId = "role-user" },
                        new IdentityUserRole<string> { UserId = "user-david-id", RoleId = "role-user" }
                    );
                    context.SaveChanges();
                }

                // 4. GRUPURI
                if (!context.Groups.Any())
                {
                    context.Groups.AddRange(
                        new Group
                        {
                            Name = "Nature Lovers",
                            Description = "A group for those who love hiking and outdoors.",
                            ModeratorId = "user-maria-id",
                            CreatedAt = DateTime.UtcNow
                        },
                        new Group
                        {
                            Name = "Tech World",
                            Description = "Discussing the latest gadgets and code.",
                            ModeratorId = "user-ioana-id",
                            CreatedAt = DateTime.UtcNow
                        },
                        new Group
                        {
                            Name = "Travel Buddies",
                            Description = "Share your travel stories and tips.",
                            ModeratorId = "user-david-id",
                            CreatedAt = DateTime.UtcNow
                        }
                    );
                    context.SaveChanges();
                }

                // 5. POSTARI
                if (!context.Posts.Any())
                {
                    context.Posts.AddRange(
                        new Post
                        {
                            UserId = "user-maria-id",
                            Title = "Beautiful Sunset",
                            Content = "Captured this amazing sunset yesterday. Nature is truly healing!",
                            CreatedAt = DateTime.UtcNow.AddDays(-2),
                            MediaUrl = "https://images.unsplash.com/photo-1472214103451-9374bd1c798e" // Placeholder image
                        },
                        new Post
                        {
                            UserId = "user-ioana-id",
                            Title = "Coding Late",
                            Content = "Debugging this project until 3 AM. Who else is up?",
                            CreatedAt = DateTime.UtcNow.AddDays(-1),
                            MediaUrl = "https://images.unsplash.com/photo-1498050108023-c5249f4df085" // Placeholder tech image
                        },
                        new Post
                        {
                            UserId = "user-david-id",
                            Title = "Next Destination?",
                            Content = "Thinking about visiting Japan next spring. Any recommendations?",
                            CreatedAt = DateTime.UtcNow,
                            MediaUrl = "https://images.unsplash.com/photo-1480796927426-f609979314bd" // Placeholder travel image
                        }
                    );
                    context.SaveChanges();
                }
            }

            // 6. ASIGURARE CA MODERATORII SUNT MEMBRI (Fix pentru bug-ul raportat)
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                var groups = context.Groups.Include(g => g.GroupMemberships).ToList();
                foreach (var group in groups)
                {
                    if (!string.IsNullOrEmpty(group.ModeratorId))
                    {
                        var isMember = group.GroupMemberships.Any(m => m.UserId == group.ModeratorId);
                        if (!isMember)
                        {
                            context.GroupMemberships.Add(new GroupMembership
                            {
                                GroupId = group.Id,
                                UserId = group.ModeratorId,
                                IsAccepted = true,
                                JoinedAt = group.CreatedAt
                            });
                        }
                    }
                }
                context.SaveChanges();
            }
        }
    }
}
