// DISABLED: Students are managed through data import
// This page is commented out to prevent manual data entry that could conflict with imported data.
// Use /Admin/Import/Untis to import Student data from UNTIS GPU010.TXT export files.

/*
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Students;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Student Student { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var student = await _context.Students
            .Include(s => s.Class)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (student == null)
        {
            return NotFound();
        }

        Student = student;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var student = await _context.Students.FindAsync(id);

        if (student != null)
        {
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
*/
