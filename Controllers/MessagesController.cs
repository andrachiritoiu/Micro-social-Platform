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
            if (user == null)
            {
                return Unauthorized();
            }

            // Get all conversations (users you've messaged or who messaged you)
            var conversations = await _context.Messages
                .Where(m => m.SenderId == user.Id || m.ReceiverId == user.Id)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            // Group by conversation partner
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
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var otherUser = await _userManager.FindByIdAsync(id);
            if (otherUser == null)
            {
                return NotFound();
            }

            // Get all messages between current user and other user
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == user.Id && m.ReceiverId == id) ||
                            (m.SenderId == id && m.ReceiverId == user.Id))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // Mark messages as read
            var unreadMessages = messages.Where(m => m.ReceiverId == user.Id && !m.IsRead).ToList();
            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }
            await _context.SaveChangesAsync();

            ViewData["OtherUser"] = otherUser;
            ViewData["CurrentUserId"] = user.Id;
            return View(messages);
        }

        // POST: Messages/Send
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Send(string receiverId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Message cannot be empty" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var receiver = await _userManager.FindByIdAsync(receiverId);
            if (receiver == null)
            {
                return Json(new { success = false, message = "Receiver not found" });
            }

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

            // Create notification for receiver
            var receiverFirstName = receiver.FirstName ?? "";
            var receiverLastName = receiver.LastName ?? "";
            var receiverFullName = $"{receiverFirstName} {receiverLastName}".Trim();
            if (string.IsNullOrEmpty(receiverFullName)) receiverFullName = receiver.UserName ?? "User";

            var senderFirstName = user.FirstName ?? "";
            var senderLastName = user.LastName ?? "";
            var senderFullName = $"{senderFirstName} {senderLastName}".Trim();
            if (string.IsNullOrEmpty(senderFullName)) senderFullName = user.UserName ?? "User";

            await NotificationsController.CreateNotification(
                _context,
                receiverId,
                "NewMessage",
                $"New message from {senderFullName}",
                content,
                $"/Messages/Conversation/{user.Id}",
                user.Id
            );

            var timeAgo = GetTimeAgo(message.CreatedAt);

            return Json(new
            {
                success = true,
                message = new
                {
                    id = message.Id,
                    content = message.Content,
                    senderId = message.SenderId,
                    receiverId = message.ReceiverId,
                    createdAt = message.CreatedAt.ToString("MMM dd, yyyy 'at' HH:mm"),
                    timeAgo = timeAgo
                }
            });
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalSeconds < 60)
                return "just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";

            return dateTime.ToString("MMM dd, yyyy");
        }
    }
}

