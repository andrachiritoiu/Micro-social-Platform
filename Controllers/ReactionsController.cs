using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MicroSocialPlatform.Controllers
{
    public class ReactionsController : Controller
    {
        //adaugare / stergere reactii
        // salvare id utilizator care a lasat reactria
        // pentru a ne asigura ca nu reactioneaza de 2 ori la aceeasi posatre

        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public ReactionsController(
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
