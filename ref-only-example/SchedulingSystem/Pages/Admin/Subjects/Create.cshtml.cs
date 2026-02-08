using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Subjects;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Subject Subject { get; set; } = new();

    public SelectList DepartmentList { get; set; } = default!;
    public SelectList RoomList { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadSelectListsAsync();

        // Set default values
        Subject.DefaultDuration = 1;
        Subject.IsActive = true;
        Subject.Color = "#007bff";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync();
            return Page();
        }

        // Check if subject code already exists
        var existingSubject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Code == Subject.Code);

        if (existingSubject != null)
        {
            ModelState.AddModelError("Subject.Code", "A subject with this code already exists.");
            await LoadSelectListsAsync();
            return Page();
        }

        _context.Subjects.Add(Subject);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Subject '{Subject.Name}' created successfully!";
        return RedirectToPage("./Index");
    }

    private async Task LoadSelectListsAsync()
    {
        var departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
        DepartmentList = new SelectList(departments, "Id", "Name");

        var rooms = await _context.Rooms.OrderBy(r => r.Name).ToListAsync();
        RoomList = new SelectList(rooms, "Id", "Name");
    }
}
