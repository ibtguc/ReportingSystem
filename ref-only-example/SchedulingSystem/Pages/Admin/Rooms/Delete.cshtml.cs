using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Rooms;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Room Room { get; set; } = new();

    public int ScheduledLessonCount { get; set; }
    public int AvailabilityCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var room = await _context.Rooms
            .Include(r => r.ScheduledLessons)
            .Include(r => r.Availabilities)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (room == null)
        {
            return NotFound();
        }

        Room = room;
        ScheduledLessonCount = room.ScheduledLessons.Count;
        AvailabilityCount = room.Availabilities.Count;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var room = await _context.Rooms
            .Include(r => r.Availabilities)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (room != null)
        {
            // Remove availability constraints
            _context.RoomAvailabilities.RemoveRange(room.Availabilities);

            // Clear room from scheduled lessons (set to null)
            var scheduledLessons = await _context.ScheduledLessons
                .Where(sl => sl.RoomId == id)
                .ToListAsync();
            foreach (var sl in scheduledLessons)
            {
                sl.RoomId = null;
            }

            // Remove the room
            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Room '{room.Name}' deleted successfully!";
        }

        return RedirectToPage("./Index");
    }
}
