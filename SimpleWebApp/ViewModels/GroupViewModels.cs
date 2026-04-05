using SimpleWebApp.Models.Enums;

namespace SimpleWebApp.ViewModels
{
    public class MyGroupsPageViewModel
    {
        public List<MyGroupsViewModel> Groups { get; set; } = new();
        public bool IsUserAdmin { get; set; }
        public bool CanJoinGroup { get; set; } = true;
    }

    public class MyGroupsViewModel
    {
        public string GroupId { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string GroupDescription { get; set; } = string.Empty;
        public GroupMemberRole Role { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GroupDetailsViewModel
    {
        public string GroupId { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string GroupDescription { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string? InviteCode { get; set; } // Only shown to admin
        public GroupMemberRole UserRole { get; set; }
        public List<GroupMemberViewModel> Members { get; set; } = new();
        public List<PendingApprovalViewModel> PendingApprovals { get; set; } = new();
    }

    public class GroupMemberViewModel
    {
        public string MemberId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public GroupMemberRole Role { get; set; }
    }

    public class PendingApprovalViewModel
    {
        public string MemberId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
    }

    public class PendingApprovalsPageViewModel
    {
        public string GroupId { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public List<PendingApprovalViewModel> PendingApprovals { get; set; } = new();
    }

    public class GroupChoiceViewModel
    {
        public bool HasGroups { get; set; }
        public int GroupCount { get; set; }
        public bool IsUserAdmin { get; set; }
    }
}
