using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Authorization; 

namespace MicroSocialPlatform.Controllers
{
    
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; 

 
        public PostsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts.Include(p => p.User).ToListAsync();
            return View(posts);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User) 
                .FirstOrDefaultAsync(m => m.Id == id);

            if (post == null) return NotFound();

            return View(post);
        }



        [Authorize(Roles = "User, Editor, Admin")] 
        public IActionResult Create()
        {
            return View();
        }


        // PostsController.cs

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User, Editor, Admin")]
        public async Task<IActionResult> Create([Bind("Title,Content,MediaUrl")] Post post)
        {
            // Pasul 1: Verifică imediat dacă utilizatorul este autentificat.
            if (!User.Identity.IsAuthenticated)
            {
                // Dacă nu ești logat, redirecționează la pagina de login sau returnează un Challenge.
                return Challenge();
            }

            // 2. Setează proprietățile interne
            post.UserId = _userManager.GetUserId(User);
            post.CreatedAt = DateTime.Now;
            post.UpdatedAt = DateTime.Now;

            // 3. Eliminăm erorile de validare pentru câmpurile setate manual
            // Chiar dacă ai rulat asta o dată, ne asigurăm că este pe poziția corectă:
            if (ModelState.ContainsKey("UserId"))
            {
                ModelState.Remove("UserId");
            }
            if (ModelState.ContainsKey("CreatedAt"))
            {
                ModelState.Remove("CreatedAt");
            }

            // 4. Verifică validitatea finală a modelului
            if (ModelState.IsValid)
            {
                _context.Add(post);
                await _context.SaveChangesAsync(); // AICI SE FACE SALVAREA!

                TempData["Message"] = "Postarea a fost adăugată cu succes!";
                return RedirectToAction(nameof(Details), new { id = post.Id });
            }

            // 5. Dacă ajunge aici, înseamnă că Title, Content sau MediaUrl a eșuat validarea.
            // Dar tu nu vezi mesajul pentru că "ModelOnly" nu afișează erorile de câmp.

            // DEBUG: Dacă tot nu merge, scoate tagul ModelOnly din View.

            // Returnăm View-ul cu erorile atașate
            return View(post);
        }

        //EDIT
        [Authorize(Roles = "User, Editor, Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

     
            string currentUserId = _userManager.GetUserId(User);

            if (post.UserId != currentUserId && !User.IsInRole("Admin") && !User.IsInRole("Editor"))
            {
                TempData["Message"] = "Nu ai permisiunea de a edita această postare.";
                return RedirectToAction(nameof(Index));
            }

            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User, Editor, Admin")] 
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,Title,Content,MediaUrl,CreatedAt")] Post post)
        {
            if (id != post.Id) return NotFound();


            if (ModelState.IsValid)
            {
                var postToUpdate = await _context.Posts.FindAsync(id);
                if (postToUpdate == null) return NotFound();

                postToUpdate.UpdatedAt = DateTime.Now;
                postToUpdate.Title = post.Title;
                postToUpdate.Content = post.Content;
                postToUpdate.MediaUrl = post.MediaUrl;

              
                await _context.SaveChangesAsync();

                TempData["Message"] = "Postarea a fost modificată cu succes!";
                return RedirectToAction(nameof(Details), new { id = post.Id });
            }
            return View(post);
        }

        //DELETE

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "User, Editor, Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
        
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                string currentUserId = _userManager.GetUserId(User);

                if (post.UserId != currentUserId && !User.IsInRole("Admin") && !User.IsInRole("Editor"))
                {
                    TempData["Message"] = "Nu ai permisiunea de a șterge această postare.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Postarea a fost ștearsă cu succes!";
            }

       
            return RedirectToAction(nameof(Index));
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.Id == id);
        }
    }
}