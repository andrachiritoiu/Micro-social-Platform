using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroSocialPlatform.Controllers
{
    public class FollowsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public FollowsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendFollowRequest(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(id) || id == user.Id)
            {
                return BadRequest("ID invalid");
            }

            var userToFollow = await _userManager.FindByIdAsync(id);
            if (userToFollow == null)
            {
                return NotFound();
            }

            var existingFollow = await _context.Follows.FirstOrDefaultAsync(f => f.FollowerId == user.Id && f.FollowedId == userToFollow.Id);

            if (existingFollow != null)
            {
                if (existingFollow.Status == "Pending" || existingFollow.Status == "Accepted")
                {
                    return RedirectToAction("Show", "Users", new { id = userToFollow.Id });
                }
            }

            if (existingFollow != null && existingFollow.Status == "Rejected")
            {
                if (userToFollow.IsPrivate)
                {
                    existingFollow.Status = "Pending";
                }
                else
                {
                    existingFollow.Status = "Accepted";
                }
                existingFollow.RequestDate = DateTime.UtcNow;
            }

            if (existingFollow == null)
            {
                var follow = new Follow
                {
                    FollowerId = user.Id,
                    FollowedId = userToFollow.Id,
                    RequestDate = DateTime.UtcNow,
                    Status = userToFollow.IsPrivate ? "Pending" : "Accepted"
                };
                _context.Follows.Add(follow);
            }

            // Verific daca notificarea exista deja
            bool notificationExists = await _context.Notifications.AnyAsync(n =>
                n.UserId == userToFollow.Id &&
                n.RelatedUserId == user.Id &&
                (n.Type == "FollowRequest" || n.Type == "NewFollower") &&
                !n.IsRead);

            if (!notificationExists)
            {
                if (userToFollow.IsPrivate)
                {
                    var notification = new Notification
                    {
                        UserId = userToFollow.Id,
                        Type = "FollowRequest",
                        Title = "Follow Request",
                        Content = "has sent you a follow request.",
                        Link = "/Follows/Requests", // Redirect la requests page
                        RelatedUserId = user.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);
                }
                else
                {
                    var notification = new Notification
                    {
                        UserId = userToFollow.Id,
                        Type = "NewFollower",
                        Title = "New Follower",
                        Content = "started following you.",
                        Link = $"/Users/Show/{user.Id}",
                        RelatedUserId = user.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Show", "Users", new { id = userToFollow.Id });
        }

        public async Task<IActionResult> Requests()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var followRequests = await _context.Follows
                .Where(f => f.FollowedId == user.Id && f.Status == "Pending")
                .Include(f => f.Follower)
                .OrderByDescending(f => f.RequestDate)
                .ToListAsync();
            return View(followRequests);
        }

        [HttpPost]
        public async Task<IActionResult> Accept(string followerId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var followRequest = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == user.Id && f.Status == "Pending");

            if (followRequest == null)
            {
                return NotFound();
            }

            followRequest.Status = "Accepted";

            // Sterg vechea "Follow Request" notificare
            var oldNotification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.UserId == user.Id &&
                                          n.RelatedUserId == followerId &&
                                          n.Type == "FollowRequest");
            if (oldNotification != null)
            {
                _context.Notifications.Remove(oldNotification);
            }

            // Creez "New Follower" notificare pentru mine(cel care a acceptat)
            var newFollowerNotif = new Notification
            {
                UserId = user.Id,
                RelatedUserId = followerId,
                Type = "NewFollower",
                Title = "New Follower",
                Content = "is now following you.",
                Link = $"/Users/Show/{followerId}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(newFollowerNotif);

            // Trimit "Follow Accepted" notificare follower-ului
            var notification = new Notification
            {
                UserId = followRequest.FollowerId,
                Type = "FollowAccepted",
                Title = "Follow Request Accepted",
                Content = "accepted your follow request.",
                Link = $"/Users/Show/{user.Id}",
                RelatedUserId = user.Id,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return RedirectToAction("Requests");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(string followerId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var followRequest = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == user.Id && f.Status == "Pending");

            if (followRequest == null)
            {
                return NotFound();
            }

            followRequest.Status = "Rejected";
            await _context.SaveChangesAsync();

            return RedirectToAction("Requests");
        }

        [HttpPost]
        public async Task<IActionResult> Unfollow(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(id) || id == user.Id)
            {
                return BadRequest("ID invalid");
            }

            var userToUnfollow = await _userManager.FindByIdAsync(id);
            if (userToUnfollow == null)
            {
                return NotFound();
            }
            var existingFollow = await _context.Follows.FirstOrDefaultAsync(f => f.FollowerId == user.Id && f.FollowedId == userToUnfollow.Id);

            if (existingFollow == null || existingFollow.Status != "Accepted")
            {
                return RedirectToAction("Show", "Users", new { id = userToUnfollow.Id });
            }
            _context.Follows.Remove(existingFollow);

            //Stergem notificare (daca dam unfollow)
            var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.UserId == userToUnfollow.Id && 
                                              n.RelatedUserId == user.Id &&    
                                              n.Type == "NewFollower");        // Tipul notificarii

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
            }


            await _context.SaveChangesAsync();
            return RedirectToAction("Show", "Users", new { id = userToUnfollow.Id });
        }

        [HttpPost]
        public async Task<IActionResult> cancelRequest(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(id) || id == user.Id)
            {
                return BadRequest("ID invalid");
            }

            var userToUnfollow = await _userManager.FindByIdAsync(id);
            if (userToUnfollow == null)
            {
                return NotFound();
            }
            var existingFollow = await _context.Follows.FirstOrDefaultAsync(f => f.FollowerId == user.Id && f.FollowedId == userToUnfollow.Id);

            if (existingFollow == null || existingFollow.Status != "Pending")
            {
                return RedirectToAction("Show", "Users", new { id = userToUnfollow.Id });
            }
            _context.Follows.Remove(existingFollow);


            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.UserId == userToUnfollow.Id &&
                                  n.RelatedUserId == user.Id &&
                                  n.Type == "FollowRequest");

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
            }


            await _context.SaveChangesAsync();
            return RedirectToAction("Show", "Users", new { id = userToUnfollow.Id });
        }
    }
}