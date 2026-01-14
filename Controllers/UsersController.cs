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
        private readonly UserManager<ApplicationUser> _userManager; 
        private readonly ApplicationDbContext _context; 
        private readonly IWebHostEnvironment _env; 
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IWebHostEnvironment env, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _context = context;
            _env = env;
            _roleManager = roleManager;
        }

        ///lista utilizatori 
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

        //cautare live 
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<object>());
            }

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
                .Take(5) 
                .ToListAsync();

            return Json(users);
        }

        //profil utilizator
        public async Task<IActionResult> Show(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var targetUser = await _context.Users
                .Include(u => u.Posts)
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (targetUser == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            bool isMe = (currentUser != null && currentUser.Id == targetUser.Id);
            bool isFollowing = false;
            string followStatus = "None"; 

            if (currentUser != null && !isMe)
            {
                var existingFollow = await _context.Follows
                    .FirstOrDefaultAsync(f => f.FollowerId == currentUser.Id && f.FollowedId == targetUser.Id);

                if (existingFollow != null)
                {
                    followStatus = existingFollow.Status;
                    if (existingFollow.Status == "Accepted")
                    {
                        isFollowing = true;
                    }
                }
            }


            bool canViewContent = isMe || !targetUser.IsPrivate || isFollowing;

            var followersCount = await _context.Follows
                .CountAsync(f => f.FollowedId == targetUser.Id && f.Status == "Accepted");

            var followingCount = await _context.Follows
                .CountAsync(f => f.FollowerId == targetUser.Id && f.Status == "Accepted");

            ViewBag.CanViewContent = canViewContent;
            ViewBag.IsMe = isMe;
            ViewBag.FollowStatus = followStatus;
            ViewBag.CurrentUserId = currentUser?.Id;
            ViewBag.FollowersCount = followersCount;
            ViewBag.FollowingCount = followingCount;

            return View(targetUser);
        }


        // editare profil
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            ApplicationUser userToEdit;

            if (string.IsNullOrEmpty(id))
            {
                userToEdit = currentUser;
            }
            else
            {
                userToEdit = await _userManager.FindByIdAsync(id);
            }

            if (userToEdit == null) return NotFound();

            if (currentUser.Id != userToEdit.Id && !User.IsInRole("Admin"))
            {
                TempData["Message"] = "Nu aveți permisiunea de a edita acest profil.";
                return RedirectToAction("Show", new { id = userToEdit.Id });
            }

            return View(userToEdit);
        }

        // salvare profil
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(string id, string FirstName, string LastName, string Description, bool IsPrivate, IFormFile? userImage)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var userToEdit = await _userManager.FindByIdAsync(id);

            if (userToEdit == null) return NotFound();

            if (currentUser.Id != userToEdit.Id && !User.IsInRole("Admin"))
            {
                TempData["Message"] = "Nu aveți permisiunea de a edita acest profil.";
                return RedirectToAction("Show", new { id = id });
            }

            bool hasOldImage = !string.IsNullOrEmpty(userToEdit.ProfileImage);
            bool hasNewImage = (userImage != null && userImage.Length > 0);

            if (!hasOldImage && !hasNewImage)
            {
                ModelState.AddModelError("", "Poza de profil este obligatorie!");
            }

            if (string.IsNullOrWhiteSpace(FirstName)) ModelState.AddModelError("FirstName", "Prenumele este obligatoriu.");
            if (string.IsNullOrWhiteSpace(LastName)) ModelState.AddModelError("LastName", "Numele este obligatoriu.");
            if (string.IsNullOrWhiteSpace(Description)) ModelState.AddModelError("Description", "Descrierea este obligatorie.");

            if (!ModelState.IsValid)
            {
                userToEdit.FirstName = FirstName;
                userToEdit.LastName = LastName;
                userToEdit.Description = Description;
                userToEdit.IsPrivate = IsPrivate;
                return View(userToEdit);
            }

            userToEdit.FirstName = FirstName;
            userToEdit.LastName = LastName;
            userToEdit.Description = Description;
            userToEdit.IsPrivate = IsPrivate;

            if (hasNewImage)
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
                TempData["Message"] = "Profilul a fost actualizat cu succes!";
                return RedirectToAction("Show", new { id = userToEdit.Id });
            }

            return View(userToEdit);
        }

        //stergere profil
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
                TempData["Message"] = "Nu aveți permisiunea de a șterge acest cont.";
                return RedirectToAction("Show", new { id = id });
            }

            var comments = _context.Comments.Where(c => c.UserId == userToDelete.Id);
            _context.Comments.RemoveRange(comments);

            var reactions = _context.Reactions.Where(r => r.UserId == userToDelete.Id);
            _context.Reactions.RemoveRange(reactions);

            var moderatedGroups = _context.Groups.Where(g => g.ModeratorId == userToDelete.Id);
            _context.Groups.RemoveRange(moderatedGroups);

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(userToDelete);

            if (result.Succeeded)
            {
                if (currentUser.Id == userToDelete.Id)
                {
                    await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(HttpContext, IdentityConstants.ApplicationScheme);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["Message"] = "Utilizatorul a fost șters.";
                    return RedirectToAction("Index"); 
                }
            }

            TempData["Message"] = "Eroare la ștergerea utilizatorului.";
            return RedirectToAction("Show", new { id = id });
        }

        //lista urmaritori
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
            ViewBag.Title = "Urmăritori";

            return View(followers);
        }

        // lista persoane urmarite
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
            ViewBag.Title = "Urmărește";

            return View(following);
        }
    }
}
