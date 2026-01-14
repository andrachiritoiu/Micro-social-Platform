using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;

namespace MicroSocialPlatform.Controllers
{
    
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // lista notificari
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var notifications = await _context.Notifications
                .Include(n => n.RelatedUser)
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        // returneaza numar notificari necitite (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { count = 0 });
            }

            var count = await _context.Notifications
                .CountAsync(n => n.UserId == user.Id && !n.IsRead);

            return Json(new { count = count });
        }

        // returneaza notificari recente (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new List<object>());
            }

            var notifications = await _context.Notifications
                .Include(n => n.RelatedUser)
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new
                {
                    id = n.Id,
                    type = n.Type,
                    title = n.Title,
                    content = n.Content,
                    link = n.Link,
                    isRead = n.IsRead,
                    createdAt = n.CreatedAt,
                    relatedUserName = n.RelatedUser != null ? n.RelatedUser.UserName : null,
                    relatedUserFullName = n.RelatedUser != null ? $"{n.RelatedUser.FirstName} {n.RelatedUser.LastName}".Trim() : null
                })
                .ToListAsync();

            return Json(notifications);
        }

        //marcheaza ca citit
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false });
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        //marcheaza toate ca citite
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        //metoda helper creare notificare
        public static async Task CreateNotification(ApplicationDbContext context, string userId, string type, string title, string? content = null, string? link = null, string? relatedUserId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Content = content,
                Link = link,
                RelatedUserId = relatedUserId,
                CreatedAt = DateTime.UtcNow
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }


        //sterge toate notificarile
        [HttpPost]
        public async Task<IActionResult> ClearAll()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .ToListAsync();

            if (notifications.Any())
            {
                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}
