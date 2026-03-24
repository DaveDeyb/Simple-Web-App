using System.ComponentModel.DataAnnotations;

namespace SimpleWebApp.Models
{
    public class TimeEntry
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public TimeEntryType EntryType { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string Project { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;
    }
}