using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Rooms;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Room> Rooms { get; set; } = new();

    public async Task OnGetAsync()
    {
        Rooms = await _context.Rooms
            .OrderBy(r => r.Building)
            .ThenBy(r => r.RoomNumber)
            .ToListAsync();
    }
}
