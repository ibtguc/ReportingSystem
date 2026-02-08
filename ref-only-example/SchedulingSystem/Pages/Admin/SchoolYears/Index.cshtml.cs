using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.SchoolYears;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<SchoolYear> SchoolYears { get; set; } = new();

    public async Task OnGetAsync()
    {
        SchoolYears = await _context.SchoolYears
            .Include(sy => sy.Terms)
            .OrderByDescending(sy => sy.StartDate)
            .ToListAsync();
    }
}
