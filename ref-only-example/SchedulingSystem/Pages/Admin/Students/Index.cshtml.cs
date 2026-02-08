using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Students;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Student> Students { get; set; } = new();

    public async Task OnGetAsync()
    {
        Students = await _context.Students
            .Include(s => s.Class)
            .OrderBy(s => s.StudentNumber)
            .ToListAsync();
    }
}
