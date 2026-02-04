using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Classes;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Class> Classes { get; set; } = new();

    public async Task OnGetAsync()
    {
        Classes = await _context.Classes
            .Include(c => c.ParentClass)
            .Include(c => c.LessonClasses)
            .OrderBy(c => c.YearLevel)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }
}
