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

            var selectedDate = date ?? DateTime.Today;
            var entries = await _service.GetEntriesForUserAsync(userId, selectedDate);

            var model = new TimeEntriesViewModel
            {
                SelectedDate = selectedDate,
                Entries = entries.OrderByDescending(e => e.Timestamp).ToList()
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
                TempData["Error"] = "Invalid time tracking sequence for your current state.";
                return RedirectToAction("Index");
            }

            await _service.AddEntryAsync(userId, entryType, project, notes);
            return RedirectToAction("Index");
        }
    }
}
