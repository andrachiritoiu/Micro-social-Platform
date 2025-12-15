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

            var user = await _context.Users
                .Include(u => u.Posts) //include posts
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }


        //afiseaza formularul de editare a profilului
        [HttpGet]
        [Authorize(Roles = "User, Admin")]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        //preia datele din formular si le salveaza in bd
        [HttpPost]
        [Authorize(Roles = "User, Admin")]
        public async Task<IActionResult> Edit(ApplicationUser requestUser, IFormFile? userImage)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return NotFound();
            }

            //actualizeaza campurile
            currentUser.FirstName = requestUser.FirstName;
            currentUser.LastName = requestUser.LastName;
            currentUser.Description = requestUser.Description;

            //upload imagine profil
            if (userImage != null && userImage.Length > 0)
            {
                //folderul de stocare
                var storagePath = Path.Combine(_env.WebRootPath, "images", "profiles");
                if(!Directory.Exists(storagePath))
                {
                    Directory.CreateDirectory(storagePath);
                }

                //generam un nume unic pentru fisier
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(userImage.FileName);
                var filePath = Path.Combine(storagePath, fileName);

                //copiem fisierul in folderul de stocare
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await userImage.CopyToAsync(stream);
                }

                //salvam calea imaginii in bd
                currentUser.ProfileImage = "/images/profiles/" + fileName;
            }

            //salvam modificarile in bd
            var result = await _userManager.UpdateAsync(currentUser);
            if (result.Succeeded)
            {
                return RedirectToAction("Show", new { id = currentUser.Id });
            }

            return View(currentUser);

        }
    }
}
