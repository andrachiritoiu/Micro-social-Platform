using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting; // Necesar pentru IWebHostEnvironment
using System.IO;                   // Necesar pentru Path.Combine și FileStream

namespace MicroSocialPlatform.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env; 

        public PostsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env; // 🛑 Setarea _env
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

        ///CREATE
        [Authorize(Roles = "User, Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Adăugat pentru securitate
        [Authorize(Roles = "User, Admin")]
        public async Task<IActionResult> Create([Bind("Title,Content")] Post post, IFormFile? UploadedMedia)
        {
            post.UserId = _userManager.GetUserId(User);
            post.CreatedAt = DateTime.Now;
            post.UpdatedAt = DateTime.Now;

            // Logica de upload media (ca în laborator)
            if (UploadedMedia != null && UploadedMedia.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };
                var fileExtension = Path.GetExtension(UploadedMedia.FileName)?.ToLower();

                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("UploadedMedia", "Fișierul trebuie să fie imagine sau video.");
                    return View(post);
                }

                var storageFolder = Path.Combine(_env.WebRootPath, "media", "posts");
                if (!Directory.Exists(storageFolder))
                {
                    Directory.CreateDirectory(storageFolder);
                }

                var fileName = Guid.NewGuid().ToString() + fileExtension;
                var filePath = Path.Combine(storageFolder, fileName);
                var databaseFileName = "/media/posts/" + fileName;

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await UploadedMedia.CopyToAsync(stream);
                }

                ModelState.Remove(nameof(post.MediaUrl));
                post.MediaUrl = databaseFileName;
            }

            // Verificarea custom: Postarea să nu fie goală
            if (string.IsNullOrWhiteSpace(post.Content) && string.IsNullOrWhiteSpace(post.MediaUrl))
            {
                ModelState.AddModelError("", "Postarea trebuie să conțină text sau un fișier media.");
                return View(post);
            }

            // Soluția garantată împotriva erorii "The User field is required."
            ModelState.Remove("UserId");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                _context.Add(post);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Post successfully added!";
                return RedirectToAction(nameof(Details), new { id = post.Id });
            }

            return View(post);
        }

        ///EDIT

        [Authorize(Roles = "User, Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            string currentUserId = _userManager.GetUserId(User);

            if (post.UserId == currentUserId || User.IsInRole("Admin") || User.IsInRole("Editor"))
            {
                return View(post);
            }

            TempData["Message"] = "You do not have permission to edit this post.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User, Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,MediaUrl")] Post post, IFormFile? UploadedMedia)
        {
            if (id != post.Id) return NotFound();

            var postToUpdate = await _context.Posts.FindAsync(id);
            if (postToUpdate == null) return NotFound();

            string currentUserId = _userManager.GetUserId(User);
            if (postToUpdate.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                TempData["Message"] = "You do not have permission to edit this post.";
                return RedirectToAction(nameof(Index));
            }

            // Handle new media upload (optional)
            if (UploadedMedia != null && UploadedMedia.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };
                var fileExtension = Path.GetExtension(UploadedMedia.FileName)?.ToLower();

                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("UploadedMedia", "Fișierul trebuie să fie imagine sau video.");
                    return View(postToUpdate);
                }

                var storageFolder = Path.Combine(_env.WebRootPath, "media", "posts");
                if (!Directory.Exists(storageFolder))
                {
                    Directory.CreateDirectory(storageFolder);
                }

                var fileName = Guid.NewGuid().ToString() + fileExtension;
                var filePath = Path.Combine(storageFolder, fileName);
                var databaseFileName = "/media/posts/" + fileName;

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await UploadedMedia.CopyToAsync(stream);
                }

                ModelState.Remove(nameof(postToUpdate.MediaUrl));
                postToUpdate.MediaUrl = databaseFileName;
            }

            if (ModelState.IsValid)
            {
                postToUpdate.UpdatedAt = DateTime.Now;
                postToUpdate.Title = post.Title;
                postToUpdate.Content = post.Content;

                // If no new file was uploaded, keep the existing MediaUrl (already on postToUpdate)

                await _context.SaveChangesAsync();
                TempData["Message"] = "Post successfully modified!";
                // după editare mergem înapoi în feed (Index)
                return RedirectToAction(nameof(Index));
            }

            return View(postToUpdate);
        }

        /// DELETE
        [Authorize(Roles = "User, Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            string currentUserId = _userManager.GetUserId(User);
            if (post.UserId == currentUserId || User.IsInRole("Admin"))
            {
                return View(post);
            }

            TempData["Message"] = "You do not have permission to delete this post.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "User, Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                TempData["Message"] = "Post not found.";
                return RedirectToAction(nameof(Index));
            }

            string currentUserId = _userManager.GetUserId(User);

            if (post.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                TempData["Message"] = "You do not have permission to delete this post.";
                return RedirectToAction(nameof(Index));
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Post deleted successfully!";

            return RedirectToAction(nameof(Index));
        }
    }
}