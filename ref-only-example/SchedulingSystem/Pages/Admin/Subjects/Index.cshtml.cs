using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Subjects;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Subject> Subjects { get; set; } = new();

    public async Task OnGetAsync()
    {
        Subjects = await _context.Subjects
            .Include(s => s.LessonSubjects)
            .OrderBy(s => s.Code)
            .ToListAsync();
    }
}
