using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Periods;

public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DeleteModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Period Period { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var period = await _context.Periods.FirstOrDefaultAsync(m => m.Id == id);

        if (period == null)
        {
            return NotFound();
        }

        Period = period;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var period = await _context.Periods.FindAsync(id);

        if (period != null)
        {
            _context.Periods.Remove(period);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
