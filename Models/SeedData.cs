using MicroSocialPlatform.Data;
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
                // Daca exista deja roluri, nu mai seed-uim
                if (context.Roles.Any())
                {
                    return;
                }

                // CREARE ROLURI
                context.Roles.AddRange(
                    new IdentityRole { Id = "role-admin", Name = "Admin", NormalizedName = "ADMIN" },
                    new IdentityRole { Id = "role-user", Name = "User", NormalizedName = "USER" }
                );

                var hasher = new PasswordHasher<ApplicationUser>();

                // CREARE UTILIZATORI
                context.Users.AddRange(
                    new ApplicationUser
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
                    },
                    new ApplicationUser
                    {
                        Id = "user1-id",
                        UserName = "user1@test.com",
                        Email = "user1@test.com",
                        NormalizedEmail = "USER1@TEST.COM",
                        NormalizedUserName = "USER1@TEST.COM",
                        FirstName = "User",
                        LastName = "One",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(null, "User1!"),
                        Description = "Default User 1",
                        ProfileImage = "/images/default.png"
                    },
                    new ApplicationUser
                    {
                        Id = "user2-id",
                        UserName = "user2@test.com",
                        Email = "user2@test.com",
                        NormalizedEmail = "USER2@TEST.COM",
                        NormalizedUserName = "USER2@TEST.COM",
                        FirstName = "User",
                        LastName = "Two",
                        EmailConfirmed = true,
                        PasswordHash = hasher.HashPassword(null, "User1!"),
                        Description = "Default User 2",
                        ProfileImage = "/images/default.png"
                    }
                );

                // ASOCIERE USER ↔ ROLE
                context.UserRoles.AddRange(
                    new IdentityUserRole<string>
                    {
                        UserId = "admin-id",
                        RoleId = "role-admin"
                    },
                    new IdentityUserRole<string>
                    {
                        UserId = "user1-id",
                        RoleId = "role-user"
                    },
                    new IdentityUserRole<string>
                    {
                        UserId = "user2-id",
                        RoleId = "role-user"
                    }
                );

                context.SaveChanges();
            }
        }
    }
}
