using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace MicroSocialPlatform.Controllers
{
    
    public class ReactionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReactionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //adauga sau sterge o reactie
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleReaction(int postId, string type)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var post = await _context.Posts
                                     .Include(p => p.Reactions)
                                     .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null) return NotFound();

            var existingReaction = post.Reactions.FirstOrDefault(r => r.UserId == currentUser.Id);

            string status = ""; 

            if (existingReaction != null)
            {
                if (existingReaction.Type == type)
                {
                    //aterge reactia existenta
                    _context.Reactions.Remove(existingReaction);
                    status = "removed";
                }
                else
                {
                    //modifica reactia
                    existingReaction.Type = type;
                    existingReaction.CreatedAt = DateTime.UtcNow;
                    status = "updated";
                }
            }
            else
            {
                //adauga reactie noua
                var newReaction = new Reaction
                {
                    UserId = currentUser.Id,
                    PostId = postId,
                    Type = type,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Reactions.Add(newReaction);
                status = "added";
            }

            await _context.SaveChangesAsync();

            //ntificare
            if (status == "added" && post.UserId != currentUser.Id)
            {
                var notification = new Notification
                {
                    UserId = post.UserId, 
                    RelatedUserId = currentUser.Id, 
                    Type = "NewReaction",
                    Title = "New Reaction",
                    Content = $"reacted {type} to your post.",
                    Link = $"/Posts/Details/{postId}", 
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            var reactionsCount = post.Reactions.Count;
            var currentReactionType = status == "removed" ? null : type;

            return Json(new { success = true, status = status, count = reactionsCount, currentType = currentReactionType });
        }


        //obține lista de reactii pentru un modal
        [HttpGet]
        public async Task<IActionResult> GetReactionsList(int postId)
        {
            var reactions = await _context.Reactions
                .Include(r => r.User) 
                .Where(r => r.PostId == postId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    userName = r.User.UserName,
                    firstName = r.User.FirstName, 
                    lastName = r.User.LastName,
                    profileImage = r.User.ProfileImage,
                    type = r.Type
                })
                .ToListAsync();

            return Json(reactions);
        }
    }
}