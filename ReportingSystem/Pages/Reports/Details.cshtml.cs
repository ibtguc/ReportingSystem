using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportingSystem.Models;
using ReportingSystem.Services;

namespace ReportingSystem.Pages.Reports;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ReportService _reportService;
    private readonly ConfidentialityService _confidentialityService;
    private readonly IWebHostEnvironment _env;

    public DetailsModel(ReportService reportService, ConfidentialityService confidentialityService, IWebHostEnvironment env)
    {
        _reportService = reportService;
        _confidentialityService = confidentialityService;
        _env = env;
    }

    public Report Report { get; set; } = null!;
    public bool CanEdit { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanReview { get; set; }
    public bool IsAuthor { get; set; }
    public List<ReportSourceLink> SourceLinks { get; set; } = new();
    public List<Report> SummariesOfThis { get; set; } = new();
    public int SummarizationDepth { get; set; }

    [BindProperty]
    public string? FeedbackComments { get; set; }

    [BindProperty]
    public string? ApprovalComments { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var report = await _reportService.GetReportByIdAsync(id);
        if (report == null) return NotFound();

        // Confidentiality access check
        if (report.IsConfidential)
        {
            var userId = GetUserId();
            if (!await _confidentialityService.CanUserAccessConfidentialItemAsync(
                Models.ConfidentialItemType.Report, id, userId))
            {
                TempData["ErrorMessage"] = "You do not have access to this confidential report.";
                return RedirectToPage("Index");
            }
        }

        Report = report;
        await ComputePermissions();

        // Phase 4: Load summarization data
        SourceLinks = await _reportService.GetSourceLinksAsync(id);
        SummariesOfThis = await _reportService.GetSummariesOfReportAsync(id);
        if (report.ReportType == ReportType.Summary || report.ReportType == ReportType.ExecutiveSummary)
            SummarizationDepth = await _reportService.GetSummarizationDepthAsync(id);

        return Page();
    }

    public async Task<IActionResult> OnPostSubmitAsync(int id)
    {
        var userId = GetUserId();
        var success = await _reportService.SubmitReportAsync(id, userId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Report submitted for review." : "Unable to submit report.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostStartReviewAsync(int id)
    {
        var userId = GetUserId();
        var success = await _reportService.StartReviewAsync(id, userId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Review started." : "Unable to start review.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRequestFeedbackAsync(int id)
    {
        var userId = GetUserId();
        var comments = FeedbackComments ?? "Feedback requested — please revise.";
        var success = await _reportService.RequestFeedbackAsync(id, userId, comments);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Feedback requested from author." : "Unable to request feedback.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var userId = GetUserId();
        var success = await _reportService.ApproveReportAsync(id, userId, ApprovalComments);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Report approved." : "Unable to approve report.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostArchiveAsync(int id)
    {
        var userId = GetUserId();
        var success = await _reportService.ArchiveReportAsync(id, userId);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Report archived." : "Unable to archive report.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveAttachmentAsync(int id, int attachmentId)
    {
        var attachment = await _reportService.GetAttachmentByIdAsync(attachmentId);
        if (attachment != null)
        {
            // Delete physical file
            var fullPath = Path.Combine(_env.WebRootPath, attachment.StoragePath);
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            await _reportService.RemoveAttachmentAsync(attachmentId);
            TempData["SuccessMessage"] = "Attachment removed.";
        }
        return RedirectToPage(new { id });
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

    private async Task ComputePermissions()
    {
        var userId = GetUserId();
        IsAuthor = Report.AuthorId == userId;
        CanEdit = IsAuthor && Report.Status == ReportStatus.Draft;
        CanSubmit = IsAuthor && (Report.Status == ReportStatus.Draft || Report.Status == ReportStatus.Revised);
        CanReview = await _reportService.CanUserReviewReportAsync(userId, Report.CommitteeId);
    }
}
