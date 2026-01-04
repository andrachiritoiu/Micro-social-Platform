using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MicroSocialPlatform.Controllers
{
    public class CommentsController : Controller
    {
        // adaugare, editare si stergere comentarii
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CommentsController(
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
        [Authorize] // orice utilizator autentificat poate adauga comentarii
        public async Task<IActionResult> New(Comment comment)
        {
            comment.Date = DateTime.Now;
            comment.UserId = _userManager.GetUserId(User);

            if (!string.IsNullOrWhiteSpace(comment.Content))
            {
                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();
                TempData["Message"] = "The comment has been added!";

                //NOTIFICARE
                var post = await _context.Posts.FindAsync(comment.PostId);
                var currentUserId = _userManager.GetUserId(User);

                //Nu trimitem notificare daca comentam la propria postare
                if (post != null && post.UserId != currentUserId)
                {
                    // Trunchiem textul pentru a nu fi prea lung in notificare
                    string previewContent = comment.Content.Length > 50
                        ? comment.Content.Substring(0, 50) + "..."
                        : comment.Content;

                    var notif = new Notification
                    {
                        UserId = post.UserId, //Proprietarul postarii primeste notificarea
                        RelatedUserId = currentUserId, //Cel care a comentat
                        Type = "NewComment",
                        Title = "New Comment",
                        Content = $"commented: \"{previewContent}\"",
                        Link = $"/Posts/Details/{post.Id}",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    _context.Notifications.Add(notif);
                    await _context.SaveChangesAsync();
                }
                // --- LOGICA NOTIFICARE (STOP) ---

                return Redirect("/Posts/Details/" + comment.PostId);
            }

            TempData["Message"] = "The comment cannot be empty.";
            return Redirect("/Posts/Details/" + comment.PostId);
        }
        [Authorize] 
        public IActionResult Edit(int id)
        {
            Comment comment = _context.Comments.Find(id);

            if (comment.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                TempData["EditingCommentId"] = id;
                return Redirect("/Posts/Details/" + comment.PostId);
            }
            else
            {
                TempData["Message"] = "You do not have permission to edit this comment.";
                return Redirect("/Posts/Details/" + comment.PostId);
            }
        }



        [HttpPost]
        [Authorize]
        public IActionResult Edit(int id, Comment commentRequest)
        {
            var comm = _context.Comments.Find(id);

            if (comm == null) return NotFound();
            if (comm.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                return Json(new { success = false, message = "You do not have permission to edit this comment." });
            }

          
            if (!string.IsNullOrWhiteSpace(commentRequest.Content))
            {
                comm.Content = commentRequest.Content;
                comm.Date = DateTime.Now;

                _context.Comments.Update(comm);
                _context.SaveChanges();

                return Json(new { success = true, newContent = comm.Content });
            }

            return Json(new { success = false, message = "The comment text cannot be empty." });
        }

        [HttpPost]
        [Authorize] // doar utilizatori autentificati (autorul sau adminul)
        public IActionResult Delete(int id)
        {
            Comment comment = _context.Comments.Find(id);

            // salvam postId inainte de stergere pentru redirectionare
            int postId = comment.PostId;

            if (comment.UserId == _userManager.GetUserId(User) ||
                User.IsInRole("Admin"))
            {
                _context.Comments.Remove(comment);
                _context.SaveChanges();
                TempData["Message"] = "The comment has been deleted!";

                return Redirect("/Posts/Details/" + postId);
            }
            else
            {
                TempData["Message"] = "You do not have permission to delete this comment.";
                return Redirect("/Posts/Details/" + postId);
            }
        }
    }
}