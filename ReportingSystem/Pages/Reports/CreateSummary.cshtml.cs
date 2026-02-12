using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Reports;

[Authorize]
public class CreateSummaryModel : PageModel
{
    private readonly ReportService _reportService;

    public CreateSummaryModel(ReportService reportService)
    {
        _reportService = reportService;
    }

    [BindProperty]
    public Report Summary { get; set; } = new();

    [BindProperty]
    public List<int> SelectedSourceIds { get; set; } = new();

    [BindProperty]
    public Dictionary<int, string?> Annotations { get; set; } = new();

    public List<Report> AvailableReports { get; set; } = new();
    public List<SelectListItem> CommitteeOptions { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? CommitteeId { get; set; }

    public async Task<IActionResult> OnGetAsync(int? committeeId)
    {
        var userId = GetUserId();

        // Only heads can create summaries
        var committees = await _reportService.GetUserCommitteesAsync(userId);
        var headCommittees = new List<Committee>();
        foreach (var c in committees)
        {
            if (await _reportService.IsUserHeadOfCommitteeAsync(userId, c.Id))
                headCommittees.Add(c);
        }

        if (!headCommittees.Any())
        {
            TempData["ErrorMessage"] = "Only committee heads can create summary reports.";
            return RedirectToPage("Index");
        }

        CommitteeOptions = headCommittees.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();

        if (committeeId.HasValue)
        {
            CommitteeId = committeeId;
            AvailableReports = await _reportService.GetSummarizableReportsAsync(committeeId.Value);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = GetUserId();

        ModelState.Remove("Summary.Author");
        ModelState.Remove("Summary.Committee");

        if (!SelectedSourceIds.Any())
        {
            ModelState.AddModelError("", "Select at least one source report to summarize.");
        }

        if (!ModelState.IsValid)
        {
            var committees = await _reportService.GetUserCommitteesAsync(userId);
            CommitteeOptions = committees.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
            if (CommitteeId.HasValue)
                AvailableReports = await _reportService.GetSummarizableReportsAsync(CommitteeId.Value);
            return Page();
        }

        Summary.ReportType = Summary.ReportType == ReportType.Detailed ? ReportType.Summary : Summary.ReportType;

        var report = await _reportService.CreateSummaryAsync(Summary, SelectedSourceIds, Annotations, userId);

        TempData["SuccessMessage"] = $"Summary \"{report.Title}\" created with {SelectedSourceIds.Count} source report(s).";
        return RedirectToPage("Details", new { id = report.Id });
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
}
