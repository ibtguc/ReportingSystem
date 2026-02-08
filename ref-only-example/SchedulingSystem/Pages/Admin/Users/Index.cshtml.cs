using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;

namespace SchedulingSystem.Pages.Admin.Users;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<User> Users { get; set; } = new();

    public async Task OnGetAsync()
    {
        Users = await _context.Users
            .OrderBy(u => u.Name)
            .ToListAsync();
    }
}
