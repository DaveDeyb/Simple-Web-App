using SimpleWebApp.Models.Enums;

namespace SimpleWebApp.ViewModels
{
    public class MyGroupsViewModel
    {
        public string GroupId { get; set; }
        public string GroupName { get; set; }
        public string GroupDescription { get; set; }
        public GroupMemberRole Role { get; set; }
        public string OwnerName { get; set; }
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GroupDetailsViewModel
    {
        public string GroupId { get; set; }
        public string GroupName { get; set; }
        public string GroupDescription { get; set; }
        public string OwnerName { get; set; }
        public string? InviteCode { get; set; } // Only shown to admin
        public GroupMemberRole UserRole { get; set; }
        public List<GroupMemberViewModel> Members { get; set; } = new();
        public List<PendingApprovalViewModel> PendingApprovals { get; set; } = new();
    }

    public class GroupMemberViewModel
    {
        public string MemberId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public GroupMemberRole Role { get; set; }
    }

    public class PendingApprovalViewModel
    {
        public string MemberId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime RequestedAt { get; set; }
    }

    public class PendingApprovalsPageViewModel
    {
        public string GroupId { get; set; }
        public string GroupName { get; set; }
        public List<PendingApprovalViewModel> PendingApprovals { get; set; } = new();
    }
}
