using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;
using System.Security.Claims;

namespace ReportingSystem.Pages.Admin.Dashboards;

public class ManagerModel : PageModel
{
    private readonly DashboardService _dashboardService;

    public ManagerModel(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public ManagerDashboardData Data { get; set; } = new();
    public User? CurrentUser { get; set; }

    public async Task OnGetAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim, out var userId))
        {
            CurrentUser = await _dashboardService.GetUserWithOrgUnitAsync(userId);
            Data = await _dashboardService.GetManagerDashboardAsync(userId, CurrentUser?.OrganizationalUnitId);
        }
    }
}
