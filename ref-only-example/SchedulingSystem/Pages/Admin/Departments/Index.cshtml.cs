using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Departments;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Department> Departments { get; set; } = new();

    public async Task OnGetAsync()
    {
        Departments = await _context.Departments
            .Include(d => d.Teachers)
            .Include(d => d.Subjects)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }
}
