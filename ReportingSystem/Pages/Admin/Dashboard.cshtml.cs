using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Admin;

public class DashboardModel : PageModel
{
    private readonly OrganizationService _orgService;
    private readonly ReportService _reportService;
    private readonly DirectiveService _directiveService;
    private readonly MeetingService _meetingService;

    public DashboardModel(OrganizationService orgService, ReportService reportService, DirectiveService directiveService, MeetingService meetingService)
    {
        _orgService = orgService;
        _reportService = reportService;
        _directiveService = directiveService;
        _meetingService = meetingService;
    }

    public (int committees, int users, int memberships, int shadows) OrgStats { get; set; }
    public (int total, int draft, int submitted, int feedbackRequested, int approved, int summarized) ReportStats { get; set; }
    public (int total, int issued, int acknowledged, int inProgress, int implemented, int overdue) DirectiveStats { get; set; }
    public (int total, int scheduled, int inProgress, int minutesReview, int finalized, int overdueActions) MeetingStats { get; set; }

    public async Task OnGetAsync()
    {
        OrgStats = await _orgService.GetOrganizationStatsAsync();
        ReportStats = await _reportService.GetReportStatsAsync();
        DirectiveStats = await _directiveService.GetDirectiveStatsAsync();
        MeetingStats = await _meetingService.GetMeetingStatsAsync();
    }
}
