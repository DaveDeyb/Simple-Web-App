namespace SimpleWebApp.Models.Enums
{
    public enum GroupMemberRole
    {
        User = 0,      // Regular user, can only track time
        Overseer = 1,  // Admin overseeing this group, can view but not manage
        Admin = 2      // Group creator/owner, full management access
    }
}
