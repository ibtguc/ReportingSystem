using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Subjects;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Subject Subject { get; set; } = new();

    public SelectList DepartmentList { get; set; } = default!;
    public SelectList RoomList { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var subject = await _context.Subjects.FindAsync(id);

        if (subject == null)
        {
            return NotFound();
        }

        Subject = subject;
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

        // Check if another subject with same code exists
        var existingSubject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Code == Subject.Code && s.Id != Subject.Id);

        if (existingSubject != null)
        {
            ModelState.AddModelError("Subject.Code", "A subject with this code already exists.");
            await LoadSelectListsAsync();
            return Page();
        }

        _context.Attach(Subject).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SubjectExists(Subject.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        TempData["SuccessMessage"] = $"Subject '{Subject.Name}' updated successfully!";
        return RedirectToPage("./Index");
    }

    private bool SubjectExists(int id)
    {
        return _context.Subjects.Any(e => e.Id == id);
    }

    private async Task LoadSelectListsAsync()
    {
        var departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
        DepartmentList = new SelectList(departments, "Id", "Name");

        var rooms = await _context.Rooms.OrderBy(r => r.Name).ToListAsync();
        RoomList = new SelectList(rooms, "Id", "Name");
    }
}
