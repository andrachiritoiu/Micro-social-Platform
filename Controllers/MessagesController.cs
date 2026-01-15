using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using MicroSocialPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroSocialPlatform.Controllers
{
    //controller mesaje private
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

        //lista conversatii
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

        //test traducere
        [AllowAnonymous]
        public async Task<IActionResult> TestTranslate()
        {
            var t = await _translation.TranslateAsync("Hello, how are you?", "ro");
            return Content(t);
        }

        //editare mesaj
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var msg = await _context.Messages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (msg == null) return NotFound();

            if (msg.SenderId != user.Id && !User.IsInRole("Admin")) return Forbid();

            ViewData["ReturnToUserId"] = msg.ReceiverId;
            return View(msg);
        }

        // procesare editare
        [HttpPost]
        public async Task<IActionResult> Edit(int id, string content, string returnToUserId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var msg = await _context.Messages.FirstOrDefaultAsync(m => m.Id == id);
            if (msg == null) return NotFound();

            if (msg.SenderId != user.Id && !User.IsInRole("Admin")) return Forbid();

            if (string.IsNullOrWhiteSpace(content))
            {
                ModelState.AddModelError("", "The message cannot be empty.");
                ViewData["ReturnToUserId"] = returnToUserId;
                return View(msg);
            }

            msg.Content = content;
            await _context.SaveChangesAsync();

            if (User.IsInRole("Admin") && msg.SenderId != user.Id)
            {
                return RedirectToAction(nameof(AdminChat), new { user1Id = msg.SenderId, user2Id = msg.ReceiverId });
            }

            return RedirectToAction(nameof(Conversation), new { id = returnToUserId });
        }

        // confirmare stergere
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var msg = await _context.Messages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (msg == null) return NotFound();

            if (msg.SenderId != user.Id && !User.IsInRole("Admin")) return Forbid();

            ViewData["ReturnToUserId"] = msg.ReceiverId;
            return View(msg);
        }

        // stergere efectiva
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id, string returnToUserId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var msg = await _context.Messages.FirstOrDefaultAsync(m => m.Id == id);
            if (msg == null) return NotFound();

            if (msg.SenderId != user.Id && !User.IsInRole("Admin")) return Forbid();

            var senderId = msg.SenderId;
            var receiverId = msg.ReceiverId;

            _context.Messages.Remove(msg);
            await _context.SaveChangesAsync();

            if (User.IsInRole("Admin") && msg.SenderId != user.Id)
            {
                return RedirectToAction(nameof(AdminChat), new { user1Id = senderId, user2Id = receiverId });
            }

            return RedirectToAction(nameof(Conversation), new { id = returnToUserId });
        }

        // ADMIN: Lista toate conversatiile
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminConversations()
        {
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var conversations = messages
                .GroupBy(m => string.Compare(m.SenderId, m.ReceiverId) < 0
                    ? (m.SenderId, m.ReceiverId)
                    : (m.ReceiverId, m.SenderId))
                .Select(g => new AdminConversationViewModel
                {
                    User1 = g.FirstOrDefault(m => m.SenderId == g.Key.Item1).Sender ?? g.FirstOrDefault(m => m.ReceiverId == g.Key.Item1).Receiver,
                    User2 = g.FirstOrDefault(m => m.SenderId == g.Key.Item2).Sender ?? g.FirstOrDefault(m => m.ReceiverId == g.Key.Item2).Receiver,
                    LastMessage = g.OrderByDescending(m => m.CreatedAt).FirstOrDefault(),
                    MessageCount = g.Count()
                })
                .OrderByDescending(c => c.LastMessage?.CreatedAt)
                .ToList();

            return View(conversations);
        }

        // ADMIN: Vezi chat complet
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminChat(string user1Id, string user2Id)
        {
            if (string.IsNullOrEmpty(user1Id) || string.IsNullOrEmpty(user2Id)) return BadRequest();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m =>
                    (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                    (m.SenderId == user2Id && m.ReceiverId == user1Id))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            ViewData["IsAdminView"] = true;
            ViewData["User1Id"] = user1Id;
            ViewData["User2Id"] = user2Id;

            var u1 = await _userManager.FindByIdAsync(user1Id);
            var u2 = await _userManager.FindByIdAsync(user2Id);
            ViewData["User1Name"] = u1?.UserName ?? "Unknown";
            ViewData["User2Name"] = u2?.UserName ?? "Unknown";
            ViewData["Title"] = $"Chat: {u1?.UserName} <-> {u2?.UserName}";

            return View("Conversation", messages);
        }

        // chat cu user
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

            // marcare citit
            var unread = messages
                .Where(m => m.ReceiverId == user.Id && !m.IsRead)
                .ToList();

            foreach (var m in unread)
                m.IsRead = true;

            await _context.SaveChangesAsync();

            // traducere
            var targetLang = "ro";
            var lastMessages = messages
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .ToList();

            var translated = new Dictionary<int, string>();

            foreach (var msg in lastMessages)
            {
                if (msg.SenderId == user.Id) continue;

                var t = await _translation.TranslateAsync(msg.Content, targetLang);
                translated[msg.Id] = t;
            }

            ViewData["Translated"] = translated;
            ViewData["OtherUser"] = otherUser;
            ViewData["CurrentUserId"] = user.Id;

            return View(messages);
        }

        // trimite mesaj (AJAX)
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
