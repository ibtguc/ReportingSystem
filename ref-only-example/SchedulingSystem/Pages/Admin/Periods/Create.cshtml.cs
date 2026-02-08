using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Periods;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Period Period { get; set; } = new();

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Validate end time is after start time
        if (Period.EndTime <= Period.StartTime)
        {
            ModelState.AddModelError("Period.EndTime", "End time must be after start time.");
            return Page();
        }

        _context.Periods.Add(Period);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
