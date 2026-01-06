using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.IO;                   

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
            _env = env; 
        }

        public async Task<IActionResult> Index()
        {
            // 1. Construim query-ul de bază (fără să aducem datele încă)
            var postsQuery = _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Comments)
                        .ThenInclude(c => c.User)
                    .Include(p => p.Reactions)
                        .ThenInclude(r => r.User)
                    .AsQueryable();

            if (User.Identity.IsAuthenticated)
            {
                //logat
                var currentUserId = _userManager.GetUserId(User);

                //persoanle pe care le urmaresti
                var followingIds = await _context.Follows
                    .Where(f => f.FollowerId == currentUserId && f.Status == "Accepted")
                    .Select(f => f.FollowedId)
                    .ToListAsync();

                //si eu
                followingIds.Add(currentUserId);

                // Filtram: Vrem postarile care au UserId in lista noastra
                postsQuery = postsQuery.Where(p => followingIds.Contains(p.UserId));
            }
            else
            {
                //nelogat
                postsQuery = postsQuery.Where(p => !p.User.IsPrivate);
            }

            var posts = await postsQuery
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

            return View(posts);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Include(p => p.Reactions)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (post == null) return NotFound();

            return View(post);
        }

        ///CREATE
        [Authorize] // orice utilizator autentificat poate crea postari
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] 
        [Authorize] 
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
        [Authorize]
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
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content")] Post post, IFormFile? UploadedMedia)
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

                postToUpdate.MediaUrl = databaseFileName;
            }

            // Remove fields from ModelState that we don't want to validate
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("MediaUrl");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("UpdatedAt");

            // Update only Title and Content
            if (ModelState.IsValid)
            {
                postToUpdate.Title = post.Title;
                postToUpdate.Content = post.Content;
                postToUpdate.UpdatedAt = DateTime.Now;

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Post successfully modified!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while saving. Please try again.");
                    return View(postToUpdate);
                }
            }

            return View(postToUpdate);
        }

        /// DELETE
        [Authorize] 
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
        [Authorize] 
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



        [HttpPost]
        [Authorize] 
        public async Task<IActionResult> AddComment([FromForm] int postId, [FromForm] string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction("Details", new { id = postId });
            }

            var user = await _userManager.GetUserAsync(User);

            var comment = new Comment
            {
                PostId = postId,
                UserId = user.Id,
                Content = content,
                CreatedAt = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = postId });
        }

    }
}