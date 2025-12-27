using MicroSocialPlatform.Data;
using MicroSocialPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MicroSocialPlatform.Controllers
{
    public class GroupsController : Controller
    {
        // creare grup, aderare grup, parasire, gestionare
        // pentru a seta un moderator id
        // pentru a permite doar moderatorului sa editeze sau stearga grupul
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public GroupsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager; // Initializează UserManager
            _roleManager = roleManager; // Initializează RoleManager
        }

        //lista grupuri
        public async Task<IActionResult> Index()
        {
            var groups = await _context.Groups
                                .Include(g => g.Moderator)
                                .OrderByDescending(g => g.CreatedAt)
                                .ToListAsync();
            return View(groups);
        }


        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Create(Group group)
        {
            ModelState.Remove("ModeratorId");
            ModelState.Remove("Moderator");

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                //Moderatorul
                group.CreatedAt = DateTime.UtcNow;
                group.ModeratorId = user.Id;
                _context.Groups.Add(group);
                await _context.SaveChangesAsync();

                //Moderatorul devine automat MEMBRU ACCEPTAT
                var membership = new GroupMembership
                {
                    GroupId = group.Id,
                    UserId = user.Id,
                    IsAccepted = true,
                    JoinedAt = DateTime.UtcNow
                };
                _context.GroupMemberships.Add(membership);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(group);
        }


        //Pagina principala a grupului
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var group = await _context.Groups
                .Include(g => g.Moderator)
                .Include(g => g.GroupPosts)
                    .ThenInclude(m => m.User) //mesajele
                .Include(g => g.GroupMemberships)
                    .ThenInclude(gm => gm.User) //membrii
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            ViewBag.IsModerator = userId == group.ModeratorId;

            var membership = group.GroupMemberships.FirstOrDefault(gm => gm.UserId == userId);
            ViewBag.UserStatus = membership == null ? "Guest" :
                                 (membership.IsAccepted ? "Member" : "Pending");

            ViewBag.CurrentUserId = userId;

            return View(group);
        }


        [HttpPost]
        public async Task<IActionResult> Join(int groupId)
        {
            var userId = _userManager.GetUserId(User);
            var existing = await _context.GroupMemberships.FindAsync(userId, groupId);

            if (existing == null)
            {
                var membership = new GroupMembership
                {
                    GroupId = groupId,
                    UserId = userId,
                    IsAccepted = false, //Asteapta aprobare
                    JoinedAt = DateTime.UtcNow
                };
                _context.GroupMemberships.Add(membership);
                await _context.SaveChangesAsync();
                TempData["Message"] = "The request has been sent to the moderator!";
            }
            return RedirectToAction("Details", new { id = groupId });
        }


        [HttpPost]
        public async Task<IActionResult> AcceptMember(int groupId, string userId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            var currentUser = _userManager.GetUserId(User);

            if (group.ModeratorId != currentUser) return Forbid(); //Doar moderatorul

            var membership = await _context.GroupMemberships.FindAsync(userId, groupId);
            if (membership != null)
            {
                membership.IsAccepted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", new { id = groupId });
        }

        //Eliminarea membrilor sau parasirea grupului
        [HttpPost]
        public async Task<IActionResult> RemoveMember(int groupId, string userId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            var currentUser = _userManager.GetUserId(User);

            //stergi ca admin sau leave ca user
            if (group.ModeratorId != currentUser && currentUser != userId) return Forbid();

            if (userId == group.ModeratorId)
            {
                TempData["Error"] = "The moderator cannot leave the group. You must delete it.";
                return RedirectToAction("Details", new { id = groupId });
            }

            var membership = await _context.GroupMemberships.FindAsync(userId, groupId);
            if (membership != null)
            {
                _context.GroupMemberships.Remove(membership);
                await _context.SaveChangesAsync();
            }

            if (currentUser == userId) return RedirectToAction("Index");
            return RedirectToAction("Details", new { id = groupId });
        }


        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var group = await _context.Groups.FindAsync(id);
            var currentUser = _userManager.GetUserId(User);

            if (group.ModeratorId != currentUser && !User.IsInRole("Admin")) return Forbid();

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> SendMessage(int groupId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return RedirectToAction("Details", new { id = groupId });

            var userId = _userManager.GetUserId(User);
            var isMember = await _context.GroupMemberships
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsAccepted);

            if (!isMember) return Forbid();

            var msg = new GroupMessage
            {
                GroupId = groupId,
                UserId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };
            _context.GroupMessages.Add(msg);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = groupId });
        }


        [HttpPost]
        public async Task<IActionResult> EditMessage(int messageId, int groupId, string newContent)
        {
            var message = await _context.GroupMessages.FindAsync(messageId);
            var currentUserId = _userManager.GetUserId(User);

            if (message == null || message.UserId != currentUserId)
            {
                return Forbid(); 
            }

            if (!string.IsNullOrWhiteSpace(newContent))
            {
                message.Content = newContent;
                
                _context.Update(message);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = groupId });
        }


        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var msg = await _context.GroupMessages.Include(m => m.Group).FirstOrDefaultAsync(m => m.Id == messageId);
            var userId = _userManager.GetUserId(User);

            if (msg == null) return NotFound();

            if (msg.UserId != userId && msg.Group.ModeratorId != userId) return Forbid();

            _context.GroupMessages.Remove(msg);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = msg.GroupId });
        }


        //Editare grup
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            //daca este moderatorul
            var group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (group.ModeratorId != currentUserId)
            {
                return Forbid(); 
            }

            return View(group);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Group group)
        {
            if (id != group.Id) return NotFound();

            var groupToUpdate = await _context.Groups.FindAsync(id);
            if (groupToUpdate == null) return NotFound();

            //moderatorul
            var currentUserId = _userManager.GetUserId(User);
            if (groupToUpdate.ModeratorId != currentUserId) return Forbid();

            groupToUpdate.Name = group.Name;
            groupToUpdate.Description = group.Description;

            // stergem erorile de validare pentru câmpurile care nu sunt în form
            ModelState.Remove("Moderator");
            ModelState.Remove("ModeratorId");
            ModelState.Remove("GroupMemberships");
            ModelState.Remove("GroupPosts");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(groupToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Groups.Any(e => e.Id == group.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Details), new { id = group.Id });
            }

            return View(group);
        }
    }
}

