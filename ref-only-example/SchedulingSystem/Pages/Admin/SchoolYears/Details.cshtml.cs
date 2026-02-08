using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.SchoolYears;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

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
}
