using SimpleWebApp.Models.Enums;

namespace SimpleWebApp.Models
{
    public class GroupMember
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string GroupId { get; set; } = string.Empty;
        public Group? Group { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        public GroupMemberRole Role { get; set; } = GroupMemberRole.User;
        public GroupMemberApprovalStatus ApprovalStatus { get; set; } = GroupMemberApprovalStatus.Approved;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; } // ID of admin who approved this member
    }
}
