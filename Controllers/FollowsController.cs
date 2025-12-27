using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MicroSocialPlatform.Controllers
{
    public class FollowsController : Controller
    {
        // urmarire / dezurmarrire utilizatori
     
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
    }
}
