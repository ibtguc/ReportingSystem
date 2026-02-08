using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin;

public class DashboardModel : PageModel
{
    private readonly OrganizationService _orgService;

    public DashboardModel(OrganizationService orgService)
    {
        _orgService = orgService;
    }

    public (int committees, int users, int memberships, int shadows) Stats { get; set; }

    public async Task OnGetAsync()
    {
        Stats = await _orgService.GetOrganizationStatsAsync();
    }
}
