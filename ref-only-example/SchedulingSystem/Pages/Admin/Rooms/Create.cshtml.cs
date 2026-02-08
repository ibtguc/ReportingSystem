using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Rooms;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Room Room { get; set; } = new();

    public SelectList DepartmentList { get; set; } = default!;
    public SelectList AlternativeRoomList { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadSelectListsAsync();

        Room.IsActive = true;
        Room.Capacity = 30;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync();
            return Page();
        }

        // Check if room number already exists
        var existingRoom = await _context.Rooms
            .FirstOrDefaultAsync(r => r.RoomNumber == Room.RoomNumber);

        if (existingRoom != null)
        {
            ModelState.AddModelError("Room.RoomNumber", "A room with this number already exists.");
            await LoadSelectListsAsync();
            return Page();
        }

        _context.Rooms.Add(Room);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Room '{Room.Name}' created successfully!";
        return RedirectToPage("./Index");
    }

    private async Task LoadSelectListsAsync()
    {
        var departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
        DepartmentList = new SelectList(departments, "Id", "Name");

        var rooms = await _context.Rooms.OrderBy(r => r.Name).ToListAsync();
        AlternativeRoomList = new SelectList(rooms, "Id", "Name");
    }
}
