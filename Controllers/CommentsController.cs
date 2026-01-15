using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroSocialPlatform.Services;

namespace MicroSocialPlatform.Controllers
{
    // gestionarea comentariilor
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ICommentValidationService _commentValidation;

        public CommentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ICommentValidationService commentValidation)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _commentValidation = commentValidation;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> New(Comment comment)
        {
            comment.UserId = _userManager.GetUserId(User);
            comment.Content = (comment.Content ?? "").Trim();

            ModelState.Remove(nameof(Comment.UserId));
            ModelState.Remove(nameof(Comment.User));
            ModelState.Remove(nameof(Comment.Post));

            if (!ModelState.IsValid)
            {
                TempData["Message"] = "The comment cannot be empty.";
                return Redirect("/Posts/Details/" + comment.PostId);
            }

            var isOk = await _commentValidation.IsCommentValidAsync(comment.Content);
            if (!isOk)
            {
                TempData["Message"] = "The comment was rejected (inappropriate content).";
                return Redirect("/Posts/Details/" + comment.PostId);
            }

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Comment added successfully!";

            var post = await _context.Posts.FindAsync(comment.PostId);
            var currentUserId = comment.UserId;

            if (post != null && post.UserId != currentUserId)
            {
                string previewContent = comment.Content.Length > 50
                    ? comment.Content.Substring(0, 50) + "..."
                    : comment.Content;

                var notif = new Notification
                {
                    UserId = post.UserId,
                    RelatedUserId = currentUserId,
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

            return Redirect("/Posts/Details/" + comment.PostId);
        }

        [Authorize]
        public IActionResult Edit(int id)
        {
            var comment = _context.Comments.Find(id);
            if (comment == null) return NotFound();

            if (comment.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(comment);
            }

            TempData["Message"] = "You do not have permission to edit this comment.";
            return Redirect("/Posts/Details/" + comment.PostId);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Edit(int id, Comment commentRequest)
        {
            var comm = _context.Comments.Find(id);
            if (comm == null) return NotFound();

            if (comm.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var newContent = (commentRequest.Content ?? "").Trim();

            if (string.IsNullOrWhiteSpace(newContent))
            {
                ModelState.AddModelError("Content", "The comment text cannot be empty.");
                return View(comm);
            }

            if (newContent.Length > 1000)
            {
                ModelState.AddModelError("Content", "The comment cannot exceed 1000 characters.");
                return View(comm);
            }

            comm.Content = newContent;

            _context.Comments.Update(comm);
            _context.SaveChanges();

            return RedirectToAction("Details", "Posts", new { id = comm.PostId });
        }

        [Authorize]
        public IActionResult Delete(int id)
        {
            var comment = _context.Comments.Find(id);
            if (comment == null) return NotFound();

            if (comment.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(comment);
            }

            TempData["Message"] = "You do not have permission to delete this comment.";
            return Redirect("/Posts/Details/" + comment.PostId);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize]
        public IActionResult DeleteConfirmed(int id)
        {
            var comment = _context.Comments.Find(id);
            if (comment == null) return NotFound();

            int postId = comment.PostId;

            if (comment.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                _context.Comments.Remove(comment);
                _context.SaveChanges();

                TempData["Message"] = "The comment has been deleted!";
                return Redirect("/Posts/Details/" + postId);
            }

            TempData["Message"] = "You do not have permission to delete this comment.";
            return Redirect("/Posts/Details/" + postId);
        }
    }
}
