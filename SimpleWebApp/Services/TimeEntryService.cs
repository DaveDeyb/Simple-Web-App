using Microsoft.EntityFrameworkCore;
using SimpleWebApp.Data;
using SimpleWebApp.Models;

namespace SimpleWebApp.Services
{
    public class TimeEntryService
    {
        private readonly ApplicationDbContext _context;

        public TimeEntryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CanAddEntryAsync(string userId, TimeEntryType type)
        {
            var lastEntry = await _context.TimeEntries
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefaultAsync();

            return (lastEntry?.EntryType, type) switch
            {
                // Start work from no entry or after clocking out
                (null, TimeEntryType.ClockIn) => true,
                (TimeEntryType.ClockOut, TimeEntryType.ClockIn) => true,
                
                // From ClockIn: can take break or clock out
                (TimeEntryType.ClockIn, TimeEntryType.BreakStart) => true,
                (TimeEntryType.ClockIn, TimeEntryType.ClockOut) => true,
                
                // From BreakStart: must end break
                (TimeEntryType.BreakStart, TimeEntryType.BreakEnd) => true,
                
                // From BreakEnd: can clock out, start new break, or resume (should be rare)
                (TimeEntryType.BreakEnd, TimeEntryType.ClockOut) => true,
                (TimeEntryType.BreakEnd, TimeEntryType.ClockIn) => true,
                (TimeEntryType.BreakEnd, TimeEntryType.BreakStart) => true,
                
                _ => false
            };
        }

        public async Task AddEntryAsync(string userId, TimeEntryType type, string? project = null, string? notes = null)
        {
            var entry = new TimeEntry
            {
                UserId = userId,
                EntryType = type,
                Timestamp = DateTime.UtcNow,
                Project = project,
                Notes = notes
            };

            _context.TimeEntries.Add(entry);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TimeEntry>> GetEntriesForUserAsync(string userId, DateTime? date = null)
        {
            var query = _context.TimeEntries.Where(e => e.UserId == userId);

            if (date.HasValue)
            {
                var start = date.Value.Date;
                var end = start.AddDays(1);
                query = query.Where(e => e.Timestamp >= start && e.Timestamp < end);
            }

            return await query.OrderBy(e => e.Timestamp).ToListAsync();
        }

        public async Task<(double totalWorked, double totalBreak)> GetTotalsForUserAsync(string userId, DateTime? date = null)
        {
            var entries = await GetEntriesForUserAsync(userId, date);
            return CalculateTotals(entries);
        }

        private static (double totalWorked, double totalBreak) CalculateTotals(List<TimeEntry> entries)
        {
            double totalWorked = 0;
            double totalBreak = 0;
            DateTime? lastWorkStart = null;
            DateTime? lastBreakStart = null;

            foreach (var entry in entries.OrderBy(e => e.Timestamp))
            {
                switch (entry.EntryType)
                {
                    case TimeEntryType.ClockIn:
                        lastWorkStart = entry.Timestamp;
                        break;
                    case TimeEntryType.BreakStart:
                        if (lastWorkStart.HasValue)
                        {
                            totalWorked += (entry.Timestamp - lastWorkStart.Value).TotalHours;
                        }
                        lastBreakStart = entry.Timestamp;
                        break;
                    case TimeEntryType.BreakEnd:
                        if (lastBreakStart.HasValue)
                        {
                            totalBreak += (entry.Timestamp - lastBreakStart.Value).TotalHours;
                        }
                        lastWorkStart = entry.Timestamp;
                        break;
                    case TimeEntryType.ClockOut:
                        if (lastWorkStart.HasValue)
                        {
                            totalWorked += (entry.Timestamp - lastWorkStart.Value).TotalHours;
                        }
                        break;
                }
            }

            return (totalWorked, totalBreak);
        }
    }
}