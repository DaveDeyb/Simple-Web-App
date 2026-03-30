using SimpleWebApp.Models;

namespace SimpleWebApp.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveGroups { get; set; }
        public int TotalHoursTracked { get; set; }
        public List<string> RecentActivity { get; set; } = new();
    }

    public class AdminUsersViewModel
    {
        public List<ApplicationUser> Users { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
    }

    public class AdminGroupsViewModel
    {
        public List<Group> Groups { get; set; } = new();
        public string CurrentUserId { get; set; } = string.Empty;
    }

    public class AdminGroupDetailsViewModel
    {
        public Group? Group { get; set; }
        public List<ApplicationUser> AvailableUsers { get; set; } = new();
    }
}
