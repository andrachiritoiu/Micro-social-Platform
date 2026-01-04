using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
<<<<<<< HEAD
=======
using Microsoft.EntityFrameworkCore;
>>>>>>> otilia/main

namespace MicroSocialPlatform.Controllers
{
    public class FollowsController : Controller
    {
        // urmarire / dezurmarrire utilizatori
<<<<<<< HEAD
     
=======

>>>>>>> otilia/main
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public FollowsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager; // Initializează UserManager
            _roleManager = roleManager; // Initializează RoleManager
        }
        public IActionResult Index()
        {
            return View();
        }
<<<<<<< HEAD
=======

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

            if (userToFollow.IsPrivate)
            {
                var notification = new Notification
                {
                    UserId = userToFollow.Id,
                    Type = "FollowRequest",
                    Title = "follow request",
                    Content = $"{user.FirstName} {user.LastName} has sent you a follow request.",
                    Link = "/Follows/Requests",
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
                    Title = "new follower",
                    Content = $"{user.FirstName} {user.LastName} is now following you.",
                    Link = $"/Users/Show/{user.Id}",
                    RelatedUserId = user.Id,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);

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
        public async Task<IActionResult> Accept(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var followRequest = await _context.Follows
                .FirstOrDefaultAsync(f => f.Id == id && f.FollowedId == user.Id && f.Status == "Pending");
            if (followRequest == null)
            {
                return NotFound();
            }
            followRequest.Status = "Accepted";


            var notification = new Notification
            {
                UserId = followRequest.FollowerId,
                Type = "FollowAccepted",
                Title = "follow request accepted",
                Content = $"{user.FirstName} {user.LastName} has accepted your follow request.",
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
        public async Task<IActionResult> Reject(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var followRequest = await _context.Follows
                .FirstOrDefaultAsync(f => f.Id == id && f.FollowedId == user.Id && f.Status == "Pending");
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
            await _context.SaveChangesAsync();
            return RedirectToAction("Show", "Users", new { id = userToUnfollow.Id });
        }
>>>>>>> otilia/main
    }
}
