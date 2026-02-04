using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Rooms;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Room Room { get; set; } = new();
    public List<RoomAvailability> Availabilities { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var room = await _context.Rooms.FirstOrDefaultAsync(m => m.Id == id);

        if (room == null)
        {
            return NotFound();
        }

        Room = room;

        // Load availability constraints
        Availabilities = await _context.RoomAvailabilities
            .Include(a => a.Period)
            .Where(a => a.RoomId == id)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.Period!.PeriodNumber)
            .ToListAsync();

        return Page();
    }
}
