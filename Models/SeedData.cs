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
                // Verificam daca in baza de date exista cel putin un rol
                if (context.Roles.Any())
                {
                    return; // baza de date contine deja roluri
                }

                // CREAREA ROLURILOR IN BD
                context.Roles.AddRange(
                    new IdentityRole { Id = "2c5e174e-3b0e-446f-86af483d56fd7210", Name = "Admin", NormalizedName = "ADMIN" },
                    new IdentityRole { Id = "2c5e174e-3b0e-446f-86af483d56fd7211", Name = "UnregisteredUser", NormalizedName = "UNREGISTEREDUSER" },
                    new IdentityRole { Id = "2c5e174e-3b0e-446f-86af483d56fd7212", Name = "User", NormalizedName = "USER" }
                );

                var hasher = new PasswordHasher<ApplicationUser>();

                // CREAREA USERILOR IN BD
                context.Users.AddRange(
                    new ApplicationUser
                    {
                        Id = "8e445865-a24d-4543-a6c6-9443d048cdb0",
                        UserName = "admin@test.com",
                        FirstName = "Admin",
                        LastName = "System",
                        EmailConfirmed = true,
                        NormalizedEmail = "ADMIN@TEST.COM",
                        Email = "admin@test.com",
                        NormalizedUserName = "ADMIN@TEST.COM",
                        PasswordHash = hasher.HashPassword(null, "Admin1!"),
                        Description = "System Administrator Account",
                        ProfileImage = "/images/default.png" // <--- Am adaugat asta
                    },
                    new ApplicationUser
                    {
                        Id = "8e445865-a24d-4543-a6c6-9443d048cdb1",
                        UserName = "unregistered@test.com",
                        FirstName = "Unregistered",
                        LastName = "User",
                        EmailConfirmed = true,
                        NormalizedEmail = "UNREGISTERED@TEST.COM",
                        Email = "unregistered@test.com",
                        NormalizedUserName = "UNREGISTERED@TEST.COM",
                        PasswordHash = hasher.HashPassword(null, "Unregistered1!"),
                        Description = "Unregistered User",
                        ProfileImage = "/images/default.png" // <--- Am adaugat asta
                    },
                    new ApplicationUser
                    {
                        Id = "8e445865-a24d-4543-a6c6-9443d048cdb2",
                        UserName = "user@test.com",
                        FirstName = "Standard",
                        LastName = "User",
                        EmailConfirmed = true,
                        NormalizedEmail = "USER@TEST.COM",
                        Email = "user@test.com",
                        NormalizedUserName = "USER@TEST.COM",
                        PasswordHash = hasher.HashPassword(null, "User1!"),
                        Description = "Standard Platform User",
                        ProfileImage = "/images/default.png" // <--- Am adaugat asta
                    }
                );

                // ASOCIEREA USER-ROLE
                context.UserRoles.AddRange(
                    new IdentityUserRole<string>
                    {
                        RoleId = "2c5e174e-3b0e-446f-86af483d56fd7210",
                        UserId = "8e445865-a24d-4543-a6c6-9443d048cdb0"
                    },
                    new IdentityUserRole<string>
                    {
                        RoleId = "2c5e174e-3b0e-446f-86af483d56fd7211",
                        UserId = "8e445865-a24d-4543-a6c6-9443d048cdb1"
                    },
                    new IdentityUserRole<string>
                    {
                        RoleId = "2c5e174e-3b0e-446f-86af483d56fd7212",
                        UserId = "8e445865-a24d-4543-a6c6-9443d048cdb2"
                    }
                );

                context.SaveChanges();
            }
        }
    }
}