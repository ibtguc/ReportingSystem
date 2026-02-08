using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.SchoolYears;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
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

        var schoolYear = await _context.SchoolYears.FindAsync(id);

        if (schoolYear == null)
        {
            return NotFound();
        }

        SchoolYear = schoolYear;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Validate that end date is after start date
        if (SchoolYear.EndDate <= SchoolYear.StartDate)
        {
            ModelState.AddModelError("SchoolYear.EndDate", "End date must be after start date.");
            return Page();
        }

        _context.Attach(SchoolYear).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SchoolYearExists(SchoolYear.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return RedirectToPage("./Index");
    }

    private bool SchoolYearExists(int id)
    {
        return _context.SchoolYears.Any(e => e.Id == id);
    }
}
