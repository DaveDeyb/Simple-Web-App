using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleWebApp.Models;
using SimpleWebApp.Services;
using SimpleWebApp.ViewModels;

namespace SimpleWebApp.Controllers
{
    [Authorize]
    public class TimeEntriesController : Controller
    {
        private readonly TimeEntryService _service;

        public TimeEntriesController(TimeEntryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? date)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewData["FullWidth"] = true;
            ViewData["BodyClass"] = "tn-dashboard-page";

            var selectedDate = date ?? DateTime.Today;
            var entries = await _service.GetEntriesForUserAsync(userId, selectedDate);
            var (totalWorked, totalBreak) = await _service.GetTotalsForUserAsync(userId, selectedDate);
            var weekDailyHours = await _service.GetWeekDailyWorkedHoursAsync(userId, selectedDate);
            var weekTotal = weekDailyHours.Sum();
            var dayIndex = ((int)selectedDate.DayOfWeek + 6) % 7;
            var weekStart = selectedDate.Date.AddDays(-dayIndex);
            var weekBreakTotal = 0d;
            for (var i = 0; i < 7; i++)
            {
                var (_, breakHours) = await _service.GetTotalsForUserAsync(userId, weekStart.AddDays(i));
                weekBreakTotal += breakHours;
            }
            var teamStatuses = await _service.GetOrganizationStatusesAsync(selectedDate);

            var model = new TimeEntriesViewModel
            {
                SelectedDate = selectedDate,
                Entries = entries.OrderByDescending(e => e.Timestamp).ToList(),
                TotalWorkedHours = totalWorked,
                TotalBreakHours = totalBreak,
                OvertimeHours = Math.Max(0, totalWorked - 8),
                WeekTotalWorkedHours = weekTotal,
                WeekTotalBreakHours = weekBreakTotal,
                WeekOvertimeHours = Math.Max(0, weekTotal - 40),
                WeekDailyHours = weekDailyHours,
                TeamMembersCount = teamStatuses.Count,
                InCount = teamStatuses.Count(s => s.Status == "In"),
                BreakCount = teamStatuses.Count(s => s.Status == "Break"),
                OutCount = teamStatuses.Count(s => s.Status == "Out"),
                TeamStatuses = teamStatuses.Take(8).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Action(TimeEntryType entryType, string project = "", string notes = "")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var canAdd = await _service.CanAddEntryAsync(userId, entryType);
            if (!canAdd)
            {
                return RedirectToAction("Index");
            }

            await _service.AddEntryAsync(userId, entryType, project, notes);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GroupRecords(string groupId, DateTime? date, string? selectedUserId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewData["FullWidth"] = true;
            ViewData["BodyClass"] = "tn-group-records-page";

            var selectedDate = date ?? DateTime.Today;
            var members = await _service.GetApprovedGroupMembersAsync(groupId);
            var hasSelectedMember = !string.IsNullOrWhiteSpace(selectedUserId)
                && members.Any(m => m.UserId == selectedUserId);

            var model = new GroupTimeEntriesViewModel
            {
                GroupId = groupId,
                SelectedDate = selectedDate,
                SelectedUserId = hasSelectedMember ? selectedUserId : null,
                Members = members,
                TimeEntries = hasSelectedMember
                    ? (await _service.GetGroupTimeEntriesAsync(groupId, selectedDate))
                        .Where(e => e.UserId == selectedUserId)
                        .ToList()
                    : new List<TimeEntry>()
            };

            return View(model);
        }
    }
}
