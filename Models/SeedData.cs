using MicroSocialPlatform.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MicroSocialPlatform.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider
       serviceProvider)
        {
            using (var context = new ApplicationDbContext(
            serviceProvider.GetRequiredService
            <DbContextOptions<ApplicationDbContext>>()))
            {
                // Verificam daca in baza de date exista cel putin un rol
                // insemnand ca a fost rulat codul
                // De aceea facem return pentru a nu insera rolurile inca o data
            // Acesta metoda trebuie sa se execute o singura data
 if (context.Roles.Any())
                {
                    return; // baza de date contine deja roluri
                }
                // CREAREA ROLURILOR IN BD
                // daca nu contine roluri, acestea se vor crea
                context.Roles.AddRange(
                new IdentityRole { Id = "2c5e174e-3b0e-446f-86af483d56fd7210", Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = "2c5e174e-3b0e-446f-86af483d56fd7211", Name = "UnregisteredUser", NormalizedName = "UNREGISTEREDUSER" },
                new IdentityRole { Id = "2c5e174e-3b0e-446f-86af483d56fd7212", Name = "User", NormalizedName = "USER" }
                );
                // o noua instanta pe care o vom utiliza pentru crearea parolelor utilizatorilor
                 // parolele sunt de tip hash
                 var hasher = new PasswordHasher<ApplicationUser>();
                // CREAREA USERILOR IN BD
                // Se creeaza cate un user pentru fiecare rol
                context.Users.AddRange(
                new ApplicationUser
                {
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb0",
                    // primary key
                    UserName = "admin@test.com",
                    FirstName = "Admin",
                    LastName = "System",
                    EmailConfirmed = true,
                    NormalizedEmail = "ADMIN@TEST.COM",
                    Email = "admin@test.com",
                    NormalizedUserName = "ADMIN@TEST.COM",
                    PasswordHash = hasher.HashPassword(null,"Admin1!")
                },
               new ApplicationUser
               {
                   Id = "8e445865-a24d-4543-a6c6-9443d048cdb1",
                   // primary key
                   UserName = "unregistered@test.com",
                   FirstName = "Unregistered",
                   LastName = "User",
                   EmailConfirmed = true,
                   NormalizedEmail = "UNREGISTERED@TEST.COM",
                   Email = "unregistered@test.com",
                   NormalizedUserName = "UNREGISTERED@TEST.COM",
                   PasswordHash = hasher.HashPassword(null,"Unregistered1!")
               },
                new ApplicationUser
                {
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb2",
                    // primary key
                    UserName = "user@test.com",
                    FirstName = "Standard",
                    LastName = "User",
                    EmailConfirmed = true,
                    NormalizedEmail = "USER@TEST.COM",
                    Email = "user@test.com",
                    NormalizedUserName = "USER@TEST.COM",
                    PasswordHash = hasher.HashPassword(null,"User1!")
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
