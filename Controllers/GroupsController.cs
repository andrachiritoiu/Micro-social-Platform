using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MicroSocialPlatform.Controllers
{
    public class GroupsController : Controller
    {
        // creare grup, aderare grup, parasire, gestionare
        // pentru a seta un moderator id
        // pentru a permite doar moderatorului sa editeze sau stearga grupul
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public GroupsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager; // Initializează UserManager
            _roleManager = roleManager; // Initializează RoleManager
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
