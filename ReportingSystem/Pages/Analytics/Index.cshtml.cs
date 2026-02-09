using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Analytics;

public class IndexModel : PageModel
{
    private readonly AnalyticsService _analyticsService;

    public IndexModel(AnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public OrganizationAnalytics Overview { get; set; } = new();
    public List<MonthlyTrend> MonthlyTrends { get; set; } = new();
    public List<CommitteeMetrics> CommitteeMetrics { get; set; } = new();
    public ComplianceMetrics Compliance { get; set; } = new();

    public async Task OnGetAsync()
    {
        Overview = await _analyticsService.GetOrganizationAnalyticsAsync();
        MonthlyTrends = await _analyticsService.GetMonthlyTrendsAsync();
        CommitteeMetrics = await _analyticsService.GetCommitteeMetricsAsync();
        Compliance = await _analyticsService.GetComplianceMetricsAsync();
    }
}
