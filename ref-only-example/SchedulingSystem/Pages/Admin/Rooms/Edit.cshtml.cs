using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Rooms;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Room Room { get; set; } = new();

    public SelectList DepartmentList { get; set; } = default!;
    public SelectList AlternativeRoomList { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var room = await _context.Rooms.FindAsync(id);

        if (room == null)
        {
            return NotFound();
        }

        Room = room;
        await LoadSelectListsAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync();
            return Page();
        }

        // Check if another room with same number exists
        var existingRoom = await _context.Rooms
            .FirstOrDefaultAsync(r => r.RoomNumber == Room.RoomNumber && r.Id != Room.Id);

        if (existingRoom != null)
        {
            ModelState.AddModelError("Room.RoomNumber", "A room with this number already exists.");
            await LoadSelectListsAsync();
            return Page();
        }

        _context.Attach(Room).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!RoomExists(Room.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        TempData["SuccessMessage"] = $"Room '{Room.Name}' updated successfully!";
        return RedirectToPage("./Index");
    }

    private bool RoomExists(int id)
    {
        return _context.Rooms.Any(e => e.Id == id);
    }

    private async Task LoadSelectListsAsync()
    {
        var departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
        DepartmentList = new SelectList(departments, "Id", "Name");

        var rooms = await _context.Rooms
            .Where(r => r.Id != Room.Id)
            .OrderBy(r => r.Name)
            .ToListAsync();
        AlternativeRoomList = new SelectList(rooms, "Id", "Name");
    }
}
