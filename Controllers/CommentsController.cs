using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MicroSocialPlatform.Controllers
{
    public class CommentsController : Controller
    {
        // adaugare, editare sj stergere comentarii
        // pentru a salva userid care a postat comentariul
        // pentru a permitte doar utilizatorului care a postat comentariul sa il editeze sau stearga
        // pentru a permite adminului sa editeze sau stearga orice comentariu
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public CommentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager; 
            _roleManager = roleManager; 
        }
        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        [Authorize(Roles = "User, Admin")]
        public IActionResult New(Comment comment)
        {
            comment.Date = DateTime.Now;

            if (ModelState.IsValid)
            {
                comment.UserId = _userManager.GetUserId(User);

                db.Comments.Add(comment);
                db.SaveChanges();
                TempData["Message"] = "Comentariul a fost adăugat!";

                return Redirect("/Posts/Details/" + comment.PostId);
            }

            //logica de validare

            return Redirect("/Posts/Details/" + comment.PostId);
        }



        [Authorize(Roles = "User, Admin")]
        public IActionResult Edit(int id)
        {
            Comment comment = db.Comments.Find(id);

 
            if (comment.UserId == _userManager.GetUserId(User) ||
                User.IsInRole("Admin"))
            {
                return View(comment);
            }
            else
            {
                TempData["Message"] = "Nu ai dreptul să editezi acest comentariu.";
                return Redirect("/Posts/Details/" + comment.PostId);
            }
        }

        [HttpPost]
        [Authorize(Roles = "User, Admin")]
        public IActionResult Edit(int id, Comment comment) 
        {
            Comment commentToUpdate = db.Comments.Find(id); 

            comment.Date = DateTime.Now;

            if (ModelState.IsValid)
            {
        
                if (commentToUpdate.UserId == _userManager.GetUserId(User) ||
                    User.IsInRole("Admin"))
                {
                    comment.PostId = commentToUpdate.PostId;
                    comment.UserId = commentToUpdate.UserId;

                    db.Comments.Update(comment);
                    db.SaveChanges();

                    TempData["Message"] = "Comentariul a fost modificat cu succes!";
                    return Redirect("/Posts/Details/" + comment.PostId);
                }
                else
                {
                    TempData["Message"] = "Nu ai dreptul să editezi acest comentariu.";
                    return Redirect("/Posts/Details/" + comment.PostId);
                }
            }

            return View(comment);
        }



        [HttpPost]
        [Authorize(Roles = "User, Admin")]
        public IActionResult Delete(int id)
        {
            Comment comment = db.Comments.Find(id);

            // Salvam postId inainte de stergere pentru redirectionare
            int postId = comment.PostId;

            if (comment.UserId == _userManager.GetUserId(User) ||
                User.IsInRole("Admin"))
            {
                db.Comments.Remove(comment);
                db.SaveChanges();
                TempData["Message"] = "Comentariul a fost șters!";
                // redirectionare la postarea de unde am sters comentariul

                return Redirect("/Posts/Details/" + postId); 
            }
            else
            {
                TempData["Message"] = "Nu ai dreptul să ștergi acest comentariu.";
                return Redirect("/Posts/Details/" + postId);
            }
        }





    }
}
