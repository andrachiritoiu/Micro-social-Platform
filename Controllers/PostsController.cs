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

        // afiseaza feed-ul cu postari
        public async Task<IActionResult> Index()
        {
            var postsQuery = _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Comments)
                        .ThenInclude(c => c.User)
                    .Include(p => p.Reactions)
                        .ThenInclude(r => r.User)
                    .AsQueryable();

            // --- AICI ESTE MODIFICAREA PENTRU ADMIN ---
            // Verificam daca esti Admin SAU daca ai emailul specific (ca siguranta)
            // MODIFICA "EMAILUL_TAU_DE_ADMIN@YAHOO.COM" CU EMAILUL TAU REAL!
            if (User.IsInRole("Admin") || User.Identity?.Name == "EMAILUL_TAU_DE_ADMIN@YAHOO.COM")
            {
                // ESTI ADMIN: Nu aplicam niciun filtru, vezi absolut toate postarile
            }
            // Daca nu esti Admin, dar esti logat -> vezi doar prietenii + postarile tale
            else if (User.Identity.IsAuthenticated)
            {
                var currentUserId = _userManager.GetUserId(User);

                var followingIds = await _context.Follows
                    .Where(f => f.FollowerId == currentUserId && f.Status == "Accepted")
                    .Select(f => f.FollowedId)
                    .ToListAsync();

                followingIds.Add(currentUserId);

                // doar postari prieteni
                postsQuery = postsQuery.Where(p => followingIds.Contains(p.UserId));
            }
            // Daca nu esti logat -> vezi doar postarile publice
            else
            {
                postsQuery = postsQuery.Where(p => !p.User.IsPrivate);
            }

            var posts = await postsQuery
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

            return View(posts);
        }

        // detalii postare
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

        // formular creare
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // salvare postare noua
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Title,Content")] Post post, IFormFile? UploadedMedia)
        {
            post.UserId = _userManager.GetUserId(User);
            post.CreatedAt = DateTime.Now;
            post.UpdatedAt = DateTime.Now;

            // upload media
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

            // validare continut (text sau media)
            if (string.IsNullOrWhiteSpace(post.Content) && string.IsNullOrWhiteSpace(post.MediaUrl))
            {
                ModelState.AddModelError("", "Postarea trebuie să conțină text sau un fișier media.");
                return View(post);
            }

            ModelState.Remove("UserId");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                _context.Add(post);
                await _context.SaveChangesAsync();

                TempData["Message"] = "The post has been added successfully!";
                return RedirectToAction(nameof(Details), new { id = post.Id });
            }

            return View(post);
        }

        // formular editare
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            string currentUserId = _userManager.GetUserId(User);

            // Verificam Admin si aici
            if (post.UserId == currentUserId || User.IsInRole("Admin") || User.Identity?.Name == "EMAILUL_TAU_DE_ADMIN@YAHOO.COM")
            {
                return View(post);
            }

            TempData["Message"] = "You do not have permission to edit this post.";
            return RedirectToAction(nameof(Index));
        }

        // salvare modificari postare
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content")] Post post, IFormFile? UploadedMedia)
        {
            if (id != post.Id) return NotFound();

            var postToUpdate = await _context.Posts.FindAsync(id);
            if (postToUpdate == null) return NotFound();

            string currentUserId = _userManager.GetUserId(User);

            // Verificam Admin si aici
            if (postToUpdate.UserId != currentUserId && !User.IsInRole("Admin") && User.Identity?.Name != "EMAILUL_TAU_DE_ADMIN@YAHOO.COM")
            {
                TempData["Message"] = "You do not have permission to edit this post.";
                return RedirectToAction(nameof(Index));
            }

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

            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("MediaUrl");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("UpdatedAt");

            if (ModelState.IsValid)
            {
                postToUpdate.Title = post.Title;
                postToUpdate.Content = post.Content;
                postToUpdate.UpdatedAt = DateTime.Now;

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "The post has been updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "A apărut o eroare la salvare.");
                    return View(postToUpdate);
                }
            }

            return View(postToUpdate);
        }

        // pagina confirmare stergere
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            string currentUserId = _userManager.GetUserId(User);

            // Verificam Admin si aici
            if (post.UserId == currentUserId || User.IsInRole("Admin") || User.Identity?.Name == "EMAILUL_TAU_DE_ADMIN@YAHOO.COM")
            {
                return View(post);
            }

            TempData["Message"] = "You do not have permission to delete this post.";
            return RedirectToAction(nameof(Index));
        }

        // confirmare stergere
        [HttpPost, ActionName("Delete")]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                TempData["Message"] = "The post was not found.";
                return RedirectToAction(nameof(Index));
            }

            string currentUserId = _userManager.GetUserId(User);

            // Verificam Admin si aici
            if (post.UserId != currentUserId && !User.IsInRole("Admin") && User.Identity?.Name != "EMAILUL_TAU_DE_ADMIN@YAHOO.COM")
            {
                TempData["Message"] = "You do not have permission to delete this post.";
                return RedirectToAction(nameof(Index));
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            TempData["Message"] = "The post has been deleted successfully!";

            return RedirectToAction(nameof(Index));
        }

        // adaugare comentariu rapid
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