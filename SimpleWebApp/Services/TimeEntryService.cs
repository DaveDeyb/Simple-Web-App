using Microsoft.EntityFrameworkCore;
using SimpleWebApp.Data;
using SimpleWebApp.Models;
using SimpleWebApp.Models.Enums;
using SimpleWebApp.ViewModels;

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
                Project = project ?? string.Empty,
                Notes = notes ?? string.Empty
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
            var includeOpenSession = date.HasValue && date.Value.Date == DateTime.UtcNow.Date;
            return CalculateTotals(entries, includeOpenSession);
        }

        public async Task<double[]> GetWeekDailyWorkedHoursAsync(string userId, DateTime referenceDate)
        {
            var dayIndex = ((int)referenceDate.DayOfWeek + 6) % 7;
            var weekStart = referenceDate.Date.AddDays(-dayIndex);
            var weekHours = new double[7];

            for (var i = 0; i < 7; i++)
            {
                var date = weekStart.AddDays(i);
                var (worked, _) = await GetTotalsForUserAsync(userId, date);
                weekHours[i] = worked;
            }

            return weekHours;
        }

        public async Task<List<TeamStatusItemViewModel>> GetOrganizationStatusesAsync(DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            var users = await _context.Users
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync();

            var entries = await _context.TimeEntries
                .Where(e => e.Timestamp >= start && e.Timestamp < end)
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync();

            var latestByUser = entries
                .GroupBy(e => e.UserId)
                .ToDictionary(g => g.Key, g => g.First());

            var statuses = users.Select(u =>
            {
                var status = "Out";
                if (latestByUser.TryGetValue(u.Id, out var lastEntry))
                {
                    status = lastEntry.EntryType switch
                    {
                        TimeEntryType.ClockIn => "In",
                        TimeEntryType.BreakEnd => "In",
                        TimeEntryType.BreakStart => "Break",
                        _ => "Out"
                    };
                }

                return new TeamStatusItemViewModel
                {
                    FullName = string.IsNullOrWhiteSpace(u.FullName) ? "Unknown" : u.FullName,
                    Status = status
                };
            }).ToList();

            return statuses;
        }

        private static (double totalWorked, double totalBreak) CalculateTotals(List<TimeEntry> entries, bool includeOpenSession)
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
                        lastWorkStart = null;
                        lastBreakStart = null;
                        break;
                }
            }

            if (includeOpenSession && lastWorkStart.HasValue)
            {
                totalWorked += (DateTime.UtcNow - lastWorkStart.Value).TotalHours;
            }

            return (totalWorked, totalBreak);
        }

        public async Task<List<TimeEntry>> GetGroupTimeEntriesAsync(string groupId, DateTime? date = null)
        {
            var baseQuery = _context.TimeEntries
                .Include(e => e.User)
                .Where(e => _context.GroupMembers.Any(gm => gm.GroupId == groupId && gm.UserId == e.UserId));

            if (date.HasValue)
            {
                var start = date.Value.Date;
                var end = start.AddDays(1);
                baseQuery = baseQuery.Where(e => e.Timestamp >= start && e.Timestamp < end);
            }

            return await baseQuery.OrderByDescending(e => e.Timestamp).ToListAsync();
        }

        public async Task<List<GroupMemberOptionViewModel>> GetApprovedGroupMembersAsync(string groupId)
        {
            return await _context.GroupMembers
                .Include(gm => gm.User)
                .Where(gm => gm.GroupId == groupId
                    && gm.ApprovalStatus == GroupMemberApprovalStatus.Approved
                    && gm.Role == GroupMemberRole.User)
                .Select(gm => new GroupMemberOptionViewModel
                {
                    UserId = gm.UserId,
                    FullName = string.IsNullOrWhiteSpace(gm.User!.FullName) ? "Unknown" : gm.User.FullName,
                    Email = gm.User!.Email ?? string.Empty
                })
                .OrderBy(m => m.FullName)
                .ToListAsync();
        }
    }
}