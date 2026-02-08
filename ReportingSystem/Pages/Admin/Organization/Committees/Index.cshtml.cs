using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Organization.Committees;

public class IndexModel : PageModel
{
    private readonly OrganizationService _orgService;

    public IndexModel(OrganizationService orgService)
    {
        _orgService = orgService;
    }

    public List<Committee> Committees { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? LevelFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SectorFilter { get; set; }

    public async Task OnGetAsync()
    {
        var all = await _orgService.GetAllCommitteesAsync();

        if (!string.IsNullOrEmpty(LevelFilter) && Enum.TryParse<HierarchyLevel>(LevelFilter, out var level))
            all = all.Where(c => c.HierarchyLevel == level).ToList();

        if (!string.IsNullOrEmpty(SectorFilter))
            all = all.Where(c => c.Sector == SectorFilter).ToList();

        Committees = all;
    }
}
