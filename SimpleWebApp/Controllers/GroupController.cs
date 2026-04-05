using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleWebApp.Data;
using SimpleWebApp.Models;
using SimpleWebApp.Models.Enums;
using SimpleWebApp.ViewModels;

namespace SimpleWebApp.Controllers
{
    [Authorize]
    public class GroupController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GroupController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Group/MyGroups
        public async Task<IActionResult> MyGroups()
        {
            var userId = _userManager.GetUserId(User);
            var userGroups = await _context.GroupMembers
                .Where(gm => gm.UserId == userId && gm.ApprovalStatus == GroupMemberApprovalStatus.Approved)
                .Include(gm => gm.Group)
                .ThenInclude(g => g!.Owner)
                .ToListAsync();

            var groupList = userGroups.Select(gm => new MyGroupsViewModel
            {
                GroupId = gm.Group!.Id,
                GroupName = gm.Group!.Name,
                GroupDescription = gm.Group!.Description,
                Role = gm.Role,
                OwnerName = GetDisplayName(gm.Group!.Owner),
                MemberCount = gm.Group!.Members.Count,
                CreatedAt = gm.Group!.CreatedAt
            }).ToList();

            var isAdmin = await IsCurrentUserAdminAsync();

            // Non-admin users with an approved group should use the time dashboard directly.
            if (!isAdmin && groupList.Count > 0)
            {
                return RedirectToAction("Index", "TimeEntries");
            }

            var pageModel = new MyGroupsPageViewModel
            {
                Groups = groupList,
                IsUserAdmin = isAdmin,
                CanJoinGroup = isAdmin || groupList.Count == 0
            };

            return View(pageModel);
        }

        // GET: Group/Choice
        [Authorize]
        public async Task<IActionResult> Choice()
        {
            var isAdmin = await IsCurrentUserAdminAsync();
            if (!isAdmin)
            {
                return Forbid();
            }

            var userId = _userManager.GetUserId(User);
            var approvedMembershipCount = await _context.GroupMembers.CountAsync(gm =>
                gm.UserId == userId && gm.ApprovalStatus == GroupMemberApprovalStatus.Approved);

            var model = new GroupChoiceViewModel
            {
                HasGroups = approvedMembershipCount > 0,
                GroupCount = approvedMembershipCount,
                IsUserAdmin = true
            };

            return View(model);
        }

        // GET: Group/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var isAdmin = await IsCurrentUserAdminAsync();
            if (!isAdmin)
            {
                return Forbid();
            }
            return View();
        }

        // POST: Group/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Name,Description")] Group group)
        {
            var isAdmin = await IsCurrentUserAdminAsync();
            if (!isAdmin)
            {
                return Forbid();
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            if (ModelState.IsValid)
            {
                group.OwnerId = userId;
                group.InviteCode = GenerateInviteCode();
                group.CreatedAt = DateTime.UtcNow;

                _context.Add(group);
                await _context.SaveChangesAsync();

                // Add the group creator as Admin member automatically
                var groupMember = new GroupMember
                {
                    GroupId = group.Id,
                    UserId = userId,
                    Role = GroupMemberRole.Admin,
                    ApprovalStatus = GroupMemberApprovalStatus.Approved,
                    ApprovedAt = DateTime.UtcNow
                };
                _context.Add(groupMember);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = group.Id });
            }

            return View(group);
        }

        // GET: Group/Join
        public async Task<IActionResult> Join()
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = await IsCurrentUserAdminAsync();
            var hasApprovedGroup = !string.IsNullOrEmpty(userId) && await _context.GroupMembers.AnyAsync(gm =>
                gm.UserId == userId && gm.ApprovalStatus == GroupMemberApprovalStatus.Approved);

            // Enforce one-group rule for non-admin users.
            if (!isAdmin && hasApprovedGroup)
            {
                return RedirectToAction("Index", "TimeEntries");
            }

            ViewBag.CancelAction = hasApprovedGroup ? "Index" : "MyGroups";
            ViewBag.CancelController = hasApprovedGroup ? "TimeEntries" : "Group";
            return View();
        }

        // POST: Group/Join
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(string inviteCode)
        {
            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                ModelState.AddModelError("inviteCode", "Invite code is required.");
                return View();
            }

            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.InviteCode == inviteCode);

            if (group == null)
            {
                ModelState.AddModelError("inviteCode", "Invalid invite code.");
                return View();
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var isAdmin = await IsCurrentUserAdminAsync();

            // Enforce one-group rule for non-admin users.
            if (!isAdmin)
            {
                var hasApprovedGroup = await _context.GroupMembers.AnyAsync(gm =>
                    gm.UserId == userId && gm.ApprovalStatus == GroupMemberApprovalStatus.Approved);

                if (hasApprovedGroup)
                {
                    return RedirectToAction("Index", "TimeEntries");
                }
            }

            // Check if user is already a member
            var existingMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == group.Id && gm.UserId == userId);

            if (existingMember != null)
            {
                ModelState.AddModelError("inviteCode", "You are already a member of this group.");
                return View();
            }

            isAdmin = isAdmin || User.HasClaim("IsAdmin", "true");
            var user = await _userManager.FindByIdAsync(userId);

            // Determine role and approval status
            GroupMemberRole role = isAdmin ? GroupMemberRole.Overseer : GroupMemberRole.User;
            GroupMemberApprovalStatus status = isAdmin ? GroupMemberApprovalStatus.Pending : GroupMemberApprovalStatus.Approved;

            var member = new GroupMember
            {
                GroupId = group.Id,
                UserId = userId,
                Role = role,
                ApprovalStatus = status,
                AddedAt = DateTime.UtcNow
            };

            _context.Add(member);
            await _context.SaveChangesAsync();

            if (isAdmin)
            {
                return RedirectToAction("PendingApprovals", new { groupId = group.Id });
            }

            return RedirectToAction(nameof(Details), new { id = group.Id });
        }

        // GET: Group/Details/5
        public async Task<IActionResult> Details(string id)
        {
            var userId = _userManager.GetUserId(User);
            var group = await _context.Groups
                .Include(g => g.Owner)
                .Include(g => g.Members)
                .ThenInclude(gm => gm.User)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            var userMembership = group.Members.FirstOrDefault(gm => gm.UserId == userId);

            if (userMembership == null)
            {
                return Forbid();
            }

            var viewModel = new GroupDetailsViewModel
            {
                GroupId = group.Id,
                GroupName = group.Name,
                GroupDescription = group.Description,
                OwnerName = GetDisplayName(group.Owner),
                InviteCode = userMembership.Role == GroupMemberRole.Admin ? group.InviteCode : null,
                UserRole = userMembership.Role,
                Members = group.Members
                    .Where(gm => gm.ApprovalStatus == GroupMemberApprovalStatus.Approved)
                    .Select(gm => new GroupMemberViewModel
                    {
                        MemberId = gm.Id,
                        UserId = gm.UserId,
                        UserName = GetDisplayName(gm.User),
                        Role = gm.Role
                    })
                    .ToList(),
                PendingApprovals = group.Members
                    .Where(gm => gm.ApprovalStatus == GroupMemberApprovalStatus.Pending && gm.Role == GroupMemberRole.Overseer)
                    .Select(gm => new PendingApprovalViewModel
                    {
                        MemberId = gm.Id,
                        UserId = gm.UserId,
                        UserName = GetDisplayName(gm.User),
                        RequestedAt = gm.AddedAt
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        // POST: Group/ApproveMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMember(string memberId, string groupId)
        {
            var userId = _userManager.GetUserId(User);
            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null || group.OwnerId != userId)
            {
                return Forbid();
            }

            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.Id == memberId);

            if (member == null)
            {
                return NotFound();
            }

            member.ApprovalStatus = GroupMemberApprovalStatus.Approved;
            member.ApprovedAt = DateTime.UtcNow;
            member.ApprovedBy = userId;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        // POST: Group/RejectMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectMember(string memberId, string groupId)
        {
            var userId = _userManager.GetUserId(User);
            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null || group.OwnerId != userId)
            {
                return Forbid();
            }

            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.Id == memberId);

            if (member == null)
            {
                return NotFound();
            }

            _context.GroupMembers.Remove(member);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        // POST: Group/RemoveMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(string memberId, string groupId)
        {
            var userId = _userManager.GetUserId(User);
            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null || group.OwnerId != userId)
            {
                return Forbid();
            }

            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.Id == memberId);

            if (member == null)
            {
                return NotFound();
            }

            _context.GroupMembers.Remove(member);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        // GET: Group/PendingApprovals/groupId
        public async Task<IActionResult> PendingApprovals(string groupId)
        {
            var userId = _userManager.GetUserId(User);
            var group = await _context.Groups
                .Include(g => g.Members)
                .ThenInclude(gm => gm.User)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null || group.OwnerId != userId)
            {
                return Forbid();
            }

            var pendingAdmins = group.Members
                .Where(gm => gm.ApprovalStatus == GroupMemberApprovalStatus.Pending && gm.Role == GroupMemberRole.Overseer)
                .Select(gm => new PendingApprovalViewModel
                {
                    MemberId = gm.Id,
                    UserId = gm.UserId,
                    UserName = GetDisplayName(gm.User),
                    RequestedAt = gm.AddedAt
                })
                .ToList();

            var viewModel = new PendingApprovalsPageViewModel
            {
                GroupId = groupId,
                GroupName = group.Name,
                PendingApprovals = pendingAdmins
            };

            return View(viewModel);
        }

        private string GenerateInviteCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Range(0, 8)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
        }

        private async Task<bool> IsCurrentUserAdminAsync()
        {
            // Prefer authoritative DB flag; keep claim as fallback for compatibility.
            var currentUser = await _userManager.GetUserAsync(User);
            return (currentUser?.IsAdmin ?? false) || User.HasClaim("IsAdmin", "true");
        }

        private static string GetDisplayName(ApplicationUser? user)
        {
            if (user == null)
            {
                return "Unknown";
            }

            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                return user.FullName;
            }

            if (!string.IsNullOrWhiteSpace(user.UserName))
            {
                return user.UserName;
            }

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                return user.Email;
            }

            return "Unknown";
        }
    }
}
