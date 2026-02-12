using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly DashboardService _dashboardService;

    public IndexModel(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public string UserRole { get; set; } = string.Empty;
    public ChairmanDashboardData? ChairmanData { get; set; }
    public OfficeDashboardData? OfficeData { get; set; }
    public CommitteeHeadDashboardData? HeadData { get; set; }
    public PersonalDashboardData PersonalData { get; set; } = null!;
    public CommitteeActivitiesData ActivitiesData { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = GetUserId();
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        UserRole = role;

        // Everyone gets the personal dashboard
        PersonalData = await _dashboardService.GetPersonalDashboardAsync(userId);

        // Committee activities (my committees + sub-committees)
        ActivitiesData = await _dashboardService.GetCommitteeActivitiesAsync(userId);

        // Role-specific dashboards
        if (role == "Chairman")
        {
            ChairmanData = await _dashboardService.GetChairmanDashboardAsync();
        }
        else if (role == "ChairmanOffice")
        {
            OfficeData = await _dashboardService.GetOfficeDashboardAsync();
        }

        // Committee heads get the head dashboard regardless of system role
        HeadData = await _dashboardService.GetCommitteeHeadDashboardAsync(userId);
        if (HeadData.ManagedCommittees.Count == 0)
            HeadData = null; // Don't show if not a head
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
}
