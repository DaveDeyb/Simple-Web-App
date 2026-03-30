namespace SimpleWebApp.Models
{
    public class GroupMember
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string GroupId { get; set; } = string.Empty;
        public Group? Group { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
