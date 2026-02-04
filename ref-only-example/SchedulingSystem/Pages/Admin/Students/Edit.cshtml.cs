// DISABLED: Students are managed through data import
// This page is commented out to prevent manual data entry that could conflict with imported data.
// Use /Admin/Import/Untis to import Student data from UNTIS GPU010.TXT export files.

/*
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Students;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Student Student { get; set; } = null!;

    public SelectList ClassList { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var student = await _context.Students.FindAsync(id);
        if (student == null)
        {
            return NotFound();
        }

        Student = student;
        await LoadClassListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadClassListAsync();
            return Page();
        }

        // Check for duplicate student number (excluding current student)
        var existingStudent = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentNumber == Student.StudentNumber && s.Id != Student.Id);

        if (existingStudent != null)
        {
            ModelState.AddModelError("Student.StudentNumber", "A student with this number already exists.");
            await LoadClassListAsync();
            return Page();
        }

        _context.Attach(Student).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await StudentExists(Student.Id))
            {
                return NotFound();
            }
            throw;
        }

        return RedirectToPage("./Index");
    }

    private async Task<bool> StudentExists(int id)
    {
        return await _context.Students.AnyAsync(e => e.Id == id);
    }

    private async Task LoadClassListAsync()
    {
        var classes = await _context.Classes
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        ClassList = new SelectList(classes, "Id", "Name");
    }
}
*/
