using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin.Dashboards;

public class ExecutiveModel : PageModel
{
    private readonly DashboardService _dashboardService;

    public ExecutiveModel(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public ExecutiveDashboardData Data { get; set; } = new();
    public ReportActivityTrend ActivityTrend { get; set; } = new();
    public UpwardFlowDistribution UpwardFlowData { get; set; } = new();

    public async Task OnGetAsync()
    {
        Data = await _dashboardService.GetExecutiveDashboardAsync();
        ActivityTrend = await _dashboardService.GetReportActivityTrendAsync(30);
        UpwardFlowData = await _dashboardService.GetUpwardFlowDistributionAsync();
    }
}
