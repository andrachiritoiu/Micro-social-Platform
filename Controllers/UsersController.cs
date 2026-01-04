using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;

namespace MicroSocialPlatform.Controllers
{
    public class UsersController : Controller
    {
        //afisare profil
        //editare profil
        // management utilizatori de catre admin

        private readonly UserManager<ApplicationUser> _userManager;  //pt gestionarea utilizatorilor
        private readonly ApplicationDbContext _context; //pt conexiunea cu baza de date
        private readonly IWebHostEnvironment _env; //pt lucrul cu fisierele
        private readonly RoleManager<IdentityRole> _roleManager;
        public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IWebHostEnvironment env, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _context = context;
            _env = env;
            _roleManager = roleManager;
        }

        //pagina pricipala de cautare
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            if (string.IsNullOrEmpty(searchString))
            {
                return View(new List<ApplicationUser>());
            }

            var users = await _context.Users
                .Where(u => u.UserName.Contains(searchString) ||
                            u.FirstName.Contains(searchString) ||
                            u.LastName.Contains(searchString))
                .ToListAsync();

            return View(users);
        }


        //motorul de cautare - Ajax
        //asyc -  nu se blocheaza asteptand raspunsul de la baza de date
        //json - cautare live
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<object>());
            }

            //interogarea bazei de date pentru utilizatori al caror nume contine termenul cautat(LINQ)
            var users = await _context.Users
                .Where(u => u.UserName.Contains(term) ||
                            u.FirstName.Contains(term) ||
                            u.LastName.Contains(term))
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    FullName = u.FirstName + " " + u.LastName,
                    u.ProfileImage
                })
                .Take(5) //ia max 5 rezultate
                .ToListAsync();

            return Json(users);
        }

        public async Task<IActionResult> Show(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

<<<<<<< HEAD
=======

>>>>>>> otilia/main
            var user = await _context.Users
                .Include(u => u.Posts) //include posts
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

<<<<<<< HEAD
=======
            var currentUser = await _userManager.GetUserAsync(User);

            string? followStatus = null;

            if (currentUser != null && currentUser.Id != user.Id)
            {
                followStatus = await _context.Follows
                    .Where(f => f.FollowerId == currentUser.Id && f.FollowedId == user.Id)
                    .Select(f => f.Status)
                    .FirstOrDefaultAsync();
            }

            ViewBag.FollowStatus = followStatus;


            var followersCount = await _context.Follows
                .CountAsync(f => f.FollowedId == user.Id && f.Status == "Accepted");

            var followingCount = await _context.Follows
                .CountAsync(f => f.FollowerId == user.Id && f.Status == "Accepted");

            ViewBag.FollowersCount = followersCount;
            ViewBag.FollowingCount = followingCount;


>>>>>>> otilia/main
            return View(user);
        }


        //afiseaza formularul de editare a profilului
        [HttpGet]
        [Authorize] // orice utilizator autentificat isi poate edita propriul profil si adminul
        public async Task<IActionResult> Edit(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            //daca id-ul este null, insemna ca editam propriul profil
            ApplicationUser userToEdit;

            if (string.IsNullOrEmpty(id))
            {
                userToEdit = currentUser;
            }
            else
            {
                userToEdit = await _userManager.FindByIdAsync(id);
            }

            if (userToEdit == null)
            {
                return NotFound();
            }

            if (currentUser.Id != userToEdit.Id && !User.IsInRole("Admin"))
            {
                TempData["Message"] = "You do not have permission to edit this profile.";
                return RedirectToAction("Show", new { id = userToEdit.Id });
            }

            return View(userToEdit);
        }

        //preia datele din formular si le salveaza in bd
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(string id, string FirstName, string LastName, string? Description, bool IsPrivate, IFormFile? userImage)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userToEdit = await _userManager.FindByIdAsync(id);

            if (userToEdit == null) return NotFound();

            if (currentUser.Id != userToEdit.Id && !User.IsInRole("Admin"))
            {
                TempData["Message"] = "You do not have permission to edit this profile.";
                return RedirectToAction("Show", new { id = id });
            }

            userToEdit.FirstName = FirstName;
            userToEdit.LastName = LastName;
            userToEdit.Description = Description;
            userToEdit.IsPrivate = IsPrivate;

            // upload imagine profil
            if (userImage != null && userImage.Length > 0)
            {
                var storagePath = Path.Combine(_env.WebRootPath, "images", "profiles");
                if (!Directory.Exists(storagePath)) Directory.CreateDirectory(storagePath);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(userImage.FileName);
                var filePath = Path.Combine(storagePath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await userImage.CopyToAsync(stream);
                }

                userToEdit.ProfileImage = "/images/profiles/" + fileName;
            }

            await _userManager.UpdateSecurityStampAsync(userToEdit);

            var result = await _userManager.UpdateAsync(userToEdit);

            if (result.Succeeded)
            {
                TempData["Message"] = "Profile updated successfully!";
                return RedirectToAction("Show", new { id = userToEdit.Id });
            }

            return View(userToEdit);
        }



        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            var userToDelete = await _userManager.FindByIdAsync(id);
            var currentUser = await _userManager.GetUserAsync(User);

            if (userToDelete == null)
            {
                return NotFound();
            }

            if (currentUser.Id != userToDelete.Id && !User.IsInRole("Admin"))
            {
                TempData["Message"] = "You do not have permission to delete this account.";
                return RedirectToAction("Show", new { id = id });
            }

            //Stergem manual datele care au DeleteBehavior.Restrict
            //Comentariile 
            var comments = _context.Comments.Where(c => c.UserId == userToDelete.Id);
            _context.Comments.RemoveRange(comments);

            //Reactiile 
            var reactions = _context.Reactions.Where(r => r.UserId == userToDelete.Id);
            _context.Reactions.RemoveRange(reactions);

            // Grupurile unde userul este Moderator 
            // Celelalte date (Postări, Mesaje, Membri, Notificări) se vor sterge singure 
            // datorită setării CASCADE din DbContext
            var moderatedGroups = _context.Groups.Where(g => g.ModeratorId == userToDelete.Id);
            _context.Groups.RemoveRange(moderatedGroups);

            await _context.SaveChangesAsync();


            var result = await _userManager.DeleteAsync(userToDelete);

            if (result.Succeeded)
            {
                //daca mi-am sters propriul cont, ma si deloghez
                if (currentUser.Id == userToDelete.Id)
                {
                    await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(HttpContext, IdentityConstants.ApplicationScheme);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // daca Adminul a sters pe altcineva
                    TempData["Message"] = "The user has been deleted.";
                    return RedirectToAction("Index"); 
                }
            }

            TempData["Message"] = "Error deleting the user.";
            return RedirectToAction("Show", new { id = id });
        }
<<<<<<< HEAD
=======
        public async Task<IActionResult> Followers(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == id);
            if (!userExists)
            {
                return NotFound();
            }

            var followers = await _context.Follows
                .Where(f => f.FollowedId == id && f.Status == "Accepted")
                .Include(f => f.Follower)
                .OrderByDescending(f => f.RequestDate)
                .Select(f => f.Follower)
                .ToListAsync();

            ViewBag.ProfileUserId = id;
            ViewBag.Title = "Followers";

            return View(followers);
        }

        [HttpGet]
        public async Task<IActionResult> Following(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == id);
            if (!userExists)
            {
                return NotFound();
            }

            var following = await _context.Follows
                .Where(f => f.FollowerId == id && f.Status == "Accepted")
                .Include(f => f.Followed)
                .OrderByDescending(f => f.RequestDate)
                .Select(f => f.Followed)
                .ToListAsync();

            ViewBag.ProfileUserId = id;
            ViewBag.Title = "Following";

            return View(following);
        }




>>>>>>> otilia/main
    }
}
