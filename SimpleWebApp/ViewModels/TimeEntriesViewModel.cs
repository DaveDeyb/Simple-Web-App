using SimpleWebApp.Models;

namespace SimpleWebApp.ViewModels
{
    public class TeamStatusItemViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Status { get; set; } = "Out";
    }

    public class TimeEntriesViewModel
    {
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public List<TimeEntry> Entries { get; set; } = new();
        public double TotalWorkedHours { get; set; }
        public double TotalBreakHours { get; set; }
        public double OvertimeHours { get; set; }
        public double WeekTotalWorkedHours { get; set; }
        public double WeekTotalBreakHours { get; set; }
        public double WeekOvertimeHours { get; set; }
        public double[] WeekDailyHours { get; set; } = new double[7];
        public int TeamMembersCount { get; set; }
        public int InCount { get; set; }
        public int BreakCount { get; set; }
        public int OutCount { get; set; }
        public List<TeamStatusItemViewModel> TeamStatuses { get; set; } = new();
    }

    public class GroupTimeEntriesViewModel
    {
        public string GroupId { get; set; } = string.Empty;
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public string? SelectedUserId { get; set; }
        public List<GroupMemberOptionViewModel> Members { get; set; } = new();
        public List<TimeEntry> TimeEntries { get; set; } = new();
    }

    public class GroupMemberOptionViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = "Unknown";
        public string Email { get; set; } = string.Empty;
    }
}
