using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleWebApp.Data;
using SimpleWebApp.Models;
using SimpleWebApp.ViewModels;

namespace SimpleWebApp.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<bool> IsAdminAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return false;

            var user = await _userManager.FindByIdAsync(userId);
            return user?.IsAdmin ?? false;
        }

        private async Task<IActionResult> CheckAdminAccessAsync()
        {
            if (!await IsAdminAsync())
            {
                return Forbid();
            }
            return null!;
        }

        // Dashboard
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var check = await CheckAdminAccessAsync();
            if (check != null) return check;

            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                ActiveGroups = await _context.Groups.CountAsync(),
                TotalHoursTracked = await _context.TimeEntries.SumAsync(t => t.EntryType == TimeEntryType.ClockOut ? 1 : 0),
                RecentActivity = new List<string>
                {
                    "System initialized",
                    $"{await _context.Users.CountAsync()} users registered",
                    $"{await _context.Groups.CountAsync()} groups created"
                }
            };

            return View(model);
        }

        // User Management
        [HttpGet]
        public async Task<IActionResult> Users(string search = "")
        {
            var check = await CheckAdminAccessAsync();
            if (check != null) return check;

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));
            }

            var users = await query.ToListAsync();

            var model = new AdminUsersViewModel
            {
                Users = users,
                SearchTerm = search
            };

            return View(model);
        }

        // Group Management
        [HttpGet]
        public async Task<IActionResult> Groups()
        {
            var check = await CheckAdminAccessAsync();
            if (check != null) return check;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var groups = await _context.Groups
                .Include(g => g.Owner)
                .Include(g => g.Members)
                .ToListAsync();

            var model = new AdminGroupsViewModel
            {
                Groups = groups,
                CurrentUserId = userId ?? ""
            };

            return View(model);
        }

        // Create Group
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup(string name, string description = "")
        {
            var check = await CheckAdminAccessAsync();
            if (check != null) return check;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Index", "Admin");

            if (string.IsNullOrEmpty(name))
            {
                TempData["Error"] = "Group name is required.";
                return RedirectToAction("Groups");
            }

            var group = new Group
            {
                Name = name,
                Description = description,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Group '{name}' created successfully!";
            return RedirectToAction("Groups");
        }

        // Delete Group
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroup(string id)
        {
            var check = await CheckAdminAccessAsync();
            if (check != null) return check;

            var group = await _context.Groups.FindAsync(id);
            if (group == null)
            {
                TempData["Error"] = "Group not found.";
                return RedirectToAction("Groups");
            }

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Group deleted successfully!";
            return RedirectToAction("Groups");
        }

        // Add User to Group
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserToGroup(string groupId, string userId)
        {
            var check = await CheckAdminAccessAsync();
            if (check != null) return check;

            var group = await _context.Groups.FindAsync(groupId);
            var user = await _userManager.FindByIdAsync(userId);

            if (group == null || user == null)
            {
                TempData["Error"] = "Group or user not found.";
                return RedirectToAction("Groups");
            }

            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (isMember)
            {
                TempData["Error"] = "User is already a member of this group.";
                return RedirectToAction("Groups");
            }

            var member = new GroupMember
            {
                GroupId = groupId,
                UserId = userId,
                AddedAt = DateTime.UtcNow
            };

            _context.GroupMembers.Add(member);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"User '{user.FullName}' added to group '{group.Name}'!";
            return RedirectToAction("Groups");
        }

        // Remove User from Group
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserFromGroup(string groupId, string userId)
        {
            var check = await CheckAdminAccessAsync();
            if (check != null) return check;

            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (member == null)
            {
                TempData["Error"] = "User is not a member of this group.";
                return RedirectToAction("Groups");
            }

            _context.GroupMembers.Remove(member);
            await _context.SaveChangesAsync();

            TempData["Success"] = "User removed from group!";
            return RedirectToAction("Groups");
        }

        // View Group Details
        [HttpGet]
        public async Task<IActionResult> GroupDetails(string id)
        {
            var check = await CheckAdminAccessAsync();
            if (check != null) return check;

            var group = await _context.Groups
                .Include(g => g.Owner)
                .Include(g => g.Members)
                .ThenInclude(gm => gm.User)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
                return NotFound();

            var allUsers = await _context.Users.ToListAsync();
            var memberIds = group.Members.Select(m => m.UserId).ToList();
            var availableUsers = allUsers.Where(u => !memberIds.Contains(u.Id)).ToList();

            var model = new AdminGroupDetailsViewModel
            {
                Group = group,
                AvailableUsers = availableUsers
            };

            return View(model);
        }
    }
}
