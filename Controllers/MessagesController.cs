using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using MicroSocialPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroSocialPlatform.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITranslationService _translation;

        public MessagesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ITranslationService translation)
        {
            _context = context;
            _userManager = userManager;
            _translation = translation;
        }

        // lista conversatii
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var conversations = await _context.Messages
                .Where(m => m.SenderId == user.Id || m.ReceiverId == user.Id)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var partners = conversations
                .Select(m => m.SenderId == user.Id ? m.Receiver : m.Sender)
                .Distinct()
                .ToList();

            ViewData["CurrentUserId"] = user.Id;
            return View(partners);
        }

        [AllowAnonymous]
        public async Task<IActionResult> TestTranslate()
        {
            var t = await _translation.TranslateAsync("Hello, how are you?", "ro");
            return Content(t);
        }


        // conversatie cu un user
        public async Task<IActionResult> Conversation(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrWhiteSpace(id)) return Unauthorized();

            var otherUser = await _userManager.FindByIdAsync(id);
            if (otherUser == null) return NotFound();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m =>
                    (m.SenderId == user.Id && m.ReceiverId == id) ||
                    (m.SenderId == id && m.ReceiverId == user.Id))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // marcam mesajele necitite ca fiind citite
            var unread = messages
                .Where(m => m.ReceiverId == user.Id && !m.IsRead)
                .ToList();

            foreach (var m in unread)
                m.IsRead = true;

            await _context.SaveChangesAsync();

            // traducere mesaje in romana
            var targetLang = "ro";

            // traducem doar ultimele 5 mesaje primite
            var lastMessages = messages
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .ToList();

            var translated = new Dictionary<int, string>();

            foreach (var msg in lastMessages)
            {
                // nu traduce mesajele trimise de userul curent
                if (msg.SenderId == user.Id) continue;

                var t = await _translation.TranslateAsync(msg.Content, targetLang);
                translated[msg.Id] = t;
            }

            ViewData["Translated"] = translated;
            ViewData["OtherUser"] = otherUser;
            ViewData["CurrentUserId"] = user.Id;

            return View(messages);
        }

        // trimitere mesaj (AJAX)
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Send(string receiverId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Json(new { success = false });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var receiver = await _userManager.FindByIdAsync(receiverId);
            if (receiver == null) return Json(new { success = false });

            var message = new Message
            {
                SenderId = user.Id,
                ReceiverId = receiverId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = new
                {
                    id = message.Id,
                    content = message.Content
                }
            });

        }
    }
}
