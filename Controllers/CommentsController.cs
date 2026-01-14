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


        //  view-ul principal
        // comentariile sunt afișate direct în pagina postării
        public IActionResult Index()
        {
            return View();
        }

        // adăugarea unui comentariu nou 
        // Este accesibilă doar utilizatorilor autentificați și primește datele comentariului prin formular (POST).
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> New(Comment comment)
        {
            // 1) Aici ne asigurăm că setăm câmpurile pe care serverul trebuie să le controleze,
            // nu le lăsăm la mâna utilizatorului din formular (de exemplu, cine este autorul).
            comment.UserId = _userManager.GetUserId(User);
            
            // Curățăm conținutul de spații inutile la început și sfârșit.
            comment.Content = (comment.Content ?? "").Trim();

            // 2) Eliminăm din validare câmpurile care nu vin din formularul de creare,
            // pentru că ModelState ar da eroare "User is required" sau "Post is required", deși noi le setăm manual.
            ModelState.Remove(nameof(Comment.UserId));
            ModelState.Remove(nameof(Comment.User));
            ModelState.Remove(nameof(Comment.Post));

            // 3) Facem verificarea standard (dacă e gol textul, dacă PostId e valid etc.)
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Comentariul nu poate fi gol.";
                return Redirect("/Posts/Details/" + comment.PostId);
            }

            // 4) Folosim serviciul nostru de AI (Gemini) pentru a verifica dacă textul este civilizat.
            // Dacă robotul zice că e de rău, respingem comentariul.
            var isOk = await _commentValidation.IsCommentValidAsync(comment.Content);
            if (!isOk)
            {
                TempData["Message"] = "Comentariul a fost respins (conținut inadecvat).";
                return Redirect("/Posts/Details/" + comment.PostId);
            }

            // 5) Totul e în regulă, așa că salvăm comentariul în baza de date.
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Comentariul a fost adăugat cu succes!";

            // 6) Trimitem o notificare proprietarului postării, ca să știe că cineva i-a scris.
            // Evident, nu-i trimitem notificare dacă și-a comentat singur la propria postare.
            var post = await _context.Posts.FindAsync(comment.PostId);
            var currentUserId = comment.UserId;

            if (post != null && post.UserId != currentUserId)
            {
                // Facem un mic rezumat al comentariului pentru notificare (primele 50 de caractere).
                string previewContent = comment.Content.Length > 50
                    ? comment.Content.Substring(0, 50) + "..."
                    : comment.Content;

                var notif = new Notification
                {
                    UserId = post.UserId,
                    RelatedUserId = currentUserId,
                    Type = "NewComment",
                    Title = "Comentariu Nou",
                    Content = $"a comentat: \"{previewContent}\"",
                    Link = $"/Posts/Details/{post.Id}",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notif);
                await _context.SaveChangesAsync();
            }

            // 7) Ne întoarcem pe pagina postării ca să vedem comentariul proaspăt adăugat.
            return Redirect("/Posts/Details/" + comment.PostId);
        }


        // Aceasta este metoda care afișează formularul de editare pentru un comentariu existent.
        // Verificăm dacă utilizatorul are dreptul să îl editeze (e autorul sau e Admin).
        [Authorize]
        public IActionResult Edit(int id)
        {
            var comment = _context.Comments.Find(id);
            
            // Dacă nu găsim comentariul, afișăm o eroare 404.
            if (comment == null) return NotFound();

            // Verificăm permisiunile: doar proprietarul sau Adminul pot edita.
            if (comment.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                // Returnăm view-ul dedicat editării acestui comentariu.
                return View(comment);
            }

            // Dacă n-are voie, îi dăm peste mână și îl trimitem înapoi.
            TempData["Message"] = "Nu aveți permisiunea de a edita acest comentariu.";
            return Redirect("/Posts/Details/" + comment.PostId);
        }

        // Aici se procesează propriu-zis modificarea comentariului (când apasă butonul "Salvează").
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Comment commentRequest)
        {
            var comm = _context.Comments.Find(id);
            if (comm == null) return NotFound();

            // Verificăm din nou permisiunile, să fim siguri că nu a "fentat" formularul.
            if (comm.UserId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Curățăm textul nou.
            var newContent = (commentRequest.Content ?? "").Trim();
            
            // Verificăm să nu fi șters tot textul din greșeală.
            if (string.IsNullOrWhiteSpace(newContent))
            {
                ModelState.AddModelError("Content", "Textul comentariului nu poate fi gol.");
                return View(comm);
            }

            // Punem și o limită de bun simț la lungime.
            if (newContent.Length > 1000)
            {
                ModelState.AddModelError("Content", "Comentariul nu poate depăși 1000 de caractere.");
                return View(comm);
            }

            // Actualizăm conținutul în obiectul din memorie.
            comm.Content = newContent;
            
            // Putem actualiza și data modificării dacă am avea un câmp pentru asta, dar momentan doar salvăm.
            // comm.UpdatedAt = DateTime.UtcNow;

            _context.Comments.Update(comm);
            _context.SaveChanges();

            // Ne întoarcem la postare să vedem ce am modificat.
            return RedirectToAction("Details", "Posts", new { id = comm.PostId });
        }

        // Metoda care afișează pagina de confirmare pentru ștergerea unui comentariu.
        // La fel, doar autorul sau Adminul au voie aici.
        [Authorize]
        public IActionResult Delete(int id)
        {
            var comment = _context.Comments.Find(id);
            if (comment == null) return NotFound();

            if (comment.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(comment);
            }

            TempData["Message"] = "Nu aveți permisiunea de a șterge acest comentariu.";
            return Redirect("/Posts/Details/" + comment.PostId);
        }

        // Ștergerea definitivă din baza de date, după ce utilizatorul confirmă.
        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var comment = _context.Comments.Find(id);
            if (comment == null) return NotFound();

            // Păstrăm ID-ul postării ca să știm unde să ne întoarcem după ce dispare comentariul.
            int postId = comment.PostId;

            if (comment.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                _context.Comments.Remove(comment);
                _context.SaveChanges();

                TempData["Message"] = "Comentariul a fost șters!";
                return Redirect("/Posts/Details/" + postId);
            }

            TempData["Message"] = "Nu aveți permisiunea de a șterge acest comentariu.";
            return Redirect("/Posts/Details/" + postId);
        }
    }
}
