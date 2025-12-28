using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;

namespace MicroSocialPlatform.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Messages
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

            var conversationPartners = conversations
                .Select(m => m.SenderId == user.Id ? m.Receiver : m.Sender)
                .Distinct()
                .ToList();

            ViewData["CurrentUserId"] = user.Id;
            return View(conversationPartners);
        }

        // GET: Messages/Conversation/{userId}
        public async Task<IActionResult> Conversation(string? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var otherUser = await _userManager.FindByIdAsync(id);
            if (otherUser == null) return NotFound();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == user.Id && m.ReceiverId == id) ||
                            (m.SenderId == id && m.ReceiverId == user.Id))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var unreadMessages = messages.Where(m => m.ReceiverId == user.Id && !m.IsRead).ToList();
            foreach (var message in unreadMessages) { message.IsRead = true; }
            await _context.SaveChangesAsync();

            ViewData["OtherUser"] = otherUser;
            ViewData["CurrentUserId"] = user.Id;
            return View(messages);
        }

        // --- CRUD MESAJ INDIVIDUAL (PAGINI SEPARATE CA LA GRUPURI) ---

        // GET: Messages/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var message = await _context.Messages.FindAsync(id);

            if (message == null || message.SenderId != user.Id) return NotFound();

            return View(message); // Deschide Views/Messages/Edit.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMessage(int id, string newContent)
        {
            var user = await _userManager.GetUserAsync(User);
            var message = await _context.Messages.FindAsync(id);

            if (message == null || message.SenderId != user.Id) return NotFound();
            if (string.IsNullOrWhiteSpace(newContent)) return RedirectToAction(nameof(Edit), new { id });

            message.Content = newContent;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Conversation), new { id = message.ReceiverId });
        }

        // GET: Messages/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var message = await _context.Messages.FindAsync(id);

            if (message == null || message.SenderId != user.Id) return NotFound();

            return View(message); // Deschide Views/Messages/Delete.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var message = await _context.Messages.FindAsync(id);

            if (message == null || message.SenderId != user.Id) return NotFound();

            var otherUserId = message.ReceiverId;
            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Conversation), new { id = otherUserId });
        }

        // --- ALTE OPERATII ---

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Send(string receiverId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrWhiteSpace(content)) return Json(new { success = false });

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

            return Json(new { success = true, message = new { id = message.Id, content = message.Content } });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConversation(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            var messages = _context.Messages.Where(m => (m.SenderId == user.Id && m.ReceiverId == id) || (m.SenderId == id && m.ReceiverId == user.Id));
            _context.Messages.RemoveRange(messages);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}