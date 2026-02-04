using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.SchoolYears;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public SchoolYear SchoolYear { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var schoolYear = await _context.SchoolYears
            .Include(sy => sy.Terms)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (schoolYear == null)
        {
            return NotFound();
        }

        SchoolYear = schoolYear;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var schoolYear = await _context.SchoolYears.FindAsync(id);

        if (schoolYear != null)
        {
            _context.SchoolYears.Remove(schoolYear);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
