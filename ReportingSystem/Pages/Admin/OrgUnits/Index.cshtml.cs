using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.OrgUnits;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<OrganizationalUnit> OrgUnits { get; set; } = new();

    public async Task OnGetAsync()
    {
        OrgUnits = await _context.OrganizationalUnits
            .Include(ou => ou.Parent)
            .Include(ou => ou.Children)
            .Include(ou => ou.Users)
            .OrderBy(ou => ou.Level)
            .ThenBy(ou => ou.SortOrder)
            .ThenBy(ou => ou.Name)
            .ToListAsync();
    }
}
