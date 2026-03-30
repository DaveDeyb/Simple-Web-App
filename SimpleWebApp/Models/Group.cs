namespace SimpleWebApp.Models
{
    public class Group
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public ApplicationUser? Owner { get; set; }
        public string InviteCode { get; set; } = string.Empty; // Random invite code for joining
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    }
}
