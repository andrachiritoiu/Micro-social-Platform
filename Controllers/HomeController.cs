using System.Diagnostics;
using MicroSocialPlatform.Models;
using MicroSocialPlatform.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; 

namespace MicroSocialPlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; 

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var postsQuery = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Reactions)
                    .ThenInclude(r => r.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .AsQueryable();

            if (User.Identity.IsAuthenticated)
            {
                var currentUserId = _userManager.GetUserId(User);

                var followingIds = await _context.Follows
                    .Where(f => f.FollowerId == currentUserId && f.Status == "Accepted")
                    .Select(f => f.FollowedId)
                    .ToListAsync();

                followingIds.Add(currentUserId);

                postsQuery = postsQuery.Where(p => followingIds.Contains(p.UserId));
            }
            else
            {
                postsQuery = postsQuery.Where(p => !p.User.IsPrivate);
            }

            var posts = await postsQuery
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(posts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}