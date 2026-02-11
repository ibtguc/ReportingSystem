using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Reports;

// Confidentiality filtering applied via ConfidentialityService

[Authorize]
public class IndexModel : PageModel
{
    private readonly ReportService _reportService;
    private readonly ConfidentialityService _confidentialityService;

    public IndexModel(ReportService reportService, ConfidentialityService confidentialityService)
    {
        _reportService = reportService;
        _confidentialityService = confidentialityService;
    }

    public List<Report> Reports { get; set; } = new();
    public List<SelectListItem> CommitteeOptions { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? CommitteeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public ReportStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public ReportType? ReportType { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowMine { get; set; }

    public async Task OnGetAsync()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

        var committees = await _reportService.GetVisibleCommitteesAsync(userId);
        CommitteeOptions = committees.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();

        int? authorFilter = ShowMine ? userId : null;

        Reports = await _reportService.GetReportsAsync(
            userId: userId,
            committeeId: CommitteeId,
            authorId: authorFilter,
            status: Status,
            reportType: ReportType);

        // Filter out confidential items the user cannot access
        Reports = await _confidentialityService.FilterAccessibleReportsAsync(Reports, userId);
    }
}
