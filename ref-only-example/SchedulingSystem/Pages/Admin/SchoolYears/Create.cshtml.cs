using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.SchoolYears;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public SchoolYear SchoolYear { get; set; } = new();

    public IActionResult OnGet()
    {
        // Set default values
        SchoolYear.IsActive = true;
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

        _context.SchoolYears.Add(SchoolYear);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
