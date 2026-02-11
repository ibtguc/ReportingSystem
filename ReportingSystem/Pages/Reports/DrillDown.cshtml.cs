using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Reports;

[Authorize]
public class DrillDownModel : PageModel
{
    private readonly ReportService _reportService;

    public DrillDownModel(ReportService reportService)
    {
        _reportService = reportService;
    }

    public Report Report { get; set; } = null!;
    public DrillDownNode Tree { get; set; } = null!;
    public int SummarizationDepth { get; set; }
    public List<Report> SummariesOfThis { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var report = await _reportService.GetReportByIdAsync(id);
        if (report == null) return NotFound();

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (!await _reportService.CanUserViewReportAsync(userId, report))
            return NotFound();

        Report = report;
        Tree = await _reportService.GetDrillDownTreeAsync(id);
        SummarizationDepth = await _reportService.GetSummarizationDepthAsync(id);
        SummariesOfThis = await _reportService.GetSummariesOfReportAsync(id);

        return Page();
    }
}
