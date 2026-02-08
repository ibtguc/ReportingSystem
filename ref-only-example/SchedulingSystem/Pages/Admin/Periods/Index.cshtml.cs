using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Periods;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Period> Periods { get; set; } = new();

    public async Task OnGetAsync()
    {
        Periods = await _context.Periods
            .OrderBy(p => p.PeriodNumber)
            .ToListAsync();
    }
}
