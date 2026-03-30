using Microsoft.AspNetCore.Identity;

namespace SimpleWebApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; } = false;
        
        // Groups owned by this user
        public ICollection<Group> OwnedGroups { get; set; } = new List<Group>();
        
        // Groups this user is a member of
        public ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
    }
}
