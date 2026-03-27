using SimpleWebApp.Models;

namespace SimpleWebApp.ViewModels
{
    public class TimeEntriesViewModel
    {
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public List<TimeEntry> Entries { get; set; } = new();
    }
}
