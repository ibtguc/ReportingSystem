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
    private readonly AuditService _auditService;
    private readonly NotificationService _notificationService;
    private readonly IWebHostEnvironment _env;

    public DetailsModel(ReportService reportService, ConfidentialityService confidentialityService,
        AuditService auditService, NotificationService notificationService, IWebHostEnvironment env)
    {
        _reportService = reportService;
        _confidentialityService = confidentialityService;
        _auditService = auditService;
        _notificationService = notificationService;
        _env = env;
    }

    public Report Report { get; set; } = null!;
    public bool CanEdit { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanReview { get; set; }
    public bool IsAuthor { get; set; }
    public bool IsHead { get; set; }
    public bool CanFinalize { get; set; }
    public bool HasAlreadyApproved { get; set; }
    public List<ReportApproval> Approvals { get; set; } = new();
    public List<User> PendingApprovers { get; set; } = new();
    public List<ReportSourceLink> SourceLinks { get; set; } = new();
    public List<Report> SummariesOfThis { get; set; } = new();
    public int SummarizationDepth { get; set; }
    public List<User> SubmitRecipients { get; set; } = new();

    [BindProperty]
    public string? FeedbackComments { get; set; }

    [BindProperty]
    public string? ApprovalComments { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var report = await _reportService.GetReportByIdAsync(id);
        if (report == null) return NotFound();

        // Visibility check — can the user see this report at all?
        var userId = GetUserId();
        if (!await _reportService.CanUserViewReportAsync(userId, report))
            return NotFound();

        // Confidentiality access check
        if (report.IsConfidential)
        {
            if (!await _confidentialityService.CanUserAccessConfidentialItemAsync(
                Models.ConfidentialItemType.Report, id, userId))
            {
                TempData["ErrorMessage"] = "You do not have access to this confidential report.";
                return RedirectToPage("Index");
            }
        }

        Report = report;
        await ComputePermissions();

        // Load approval tracking
        Approvals = report.Approvals.OrderBy(a => a.ApprovedAt).ToList();
        PendingApprovers = await _reportService.GetPendingApproversAsync(id);
        CanFinalize = IsHead && report.Status == ReportStatus.Submitted
            && await _reportService.CanHeadFinalizeAsync(id);
        HasAlreadyApproved = Approvals.Any(a => a.UserId == GetUserId());

        // Phase 4: Load summarization data
        SourceLinks = await _reportService.GetSourceLinksAsync(id);
        SummariesOfThis = await _reportService.GetSummariesOfReportAsync(id);
        if (report.ReportType == ReportType.Summary || report.ReportType == ReportType.ExecutiveSummary)
            SummarizationDepth = await _reportService.GetSummarizationDepthAsync(id);

        // Load notification recipients for the submit button display
        if (CanSubmit)
            SubmitRecipients = await _notificationService.GetReportSubmissionRecipientsAsync(report.CommitteeId, report.AuthorId);

        return Page();
    }

    public async Task<IActionResult> OnPostSubmitAsync(int id)
    {
        var userId = GetUserId();
        var report = await _reportService.GetReportByIdAsync(id);
        var success = await _reportService.SubmitReportAsync(id, userId);
        if (success && report != null)
        {
            var oldStatus = report.Status == ReportStatus.FeedbackRequested ? "FeedbackRequested" : "Draft";
            await _auditService.LogStatusChangeAsync("Report", id, null, oldStatus, "Submitted", userId, User.Identity?.Name);
            await _notificationService.NotifyReportSubmittedAsync(id, report.Title, report.AuthorId, report.CommitteeId);
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Report submitted for collective approval." : "Unable to submit report.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRequestFeedbackAsync(int id)
    {
        var userId = GetUserId();
        var comments = FeedbackComments ?? "Feedback requested — please revise.";
        var report = await _reportService.GetReportByIdAsync(id);
        var success = await _reportService.RequestFeedbackAsync(id, userId, comments);
        if (success && report != null)
        {
            await _auditService.LogStatusChangeAsync("Report", id, null, "Submitted", "FeedbackRequested", userId, User.Identity?.Name, comments);
            await _notificationService.NotifyReportStatusChangedAsync(id, report.Title, report.AuthorId, "Feedback Requested");
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Feedback requested from author." : "Unable to request feedback.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var userId = GetUserId();
        var report = await _reportService.GetReportByIdAsync(id);
        var success = await _reportService.ApproveByMemberAsync(id, userId, ApprovalComments);
        if (success && report != null)
        {
            await _auditService.LogAsync(AuditActionType.ReportApproved, "Report", id,
                userId: userId, userName: User.Identity?.Name, details: ApprovalComments ?? "Member approval");

            // Check if report has moved to Approved (all members approved)
            var updated = await _reportService.GetReportByIdAsync(id);
            if (updated?.Status == ReportStatus.Approved)
            {
                await _notificationService.NotifyReportStatusChangedAsync(id, report.Title, report.AuthorId, "Approved");
            }
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Your approval has been recorded." : "Unable to approve report.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostFinalizeAsync(int id)
    {
        var userId = GetUserId();
        var report = await _reportService.GetReportByIdAsync(id);
        var success = await _reportService.FinalizeByHeadAsync(id, userId, ApprovalComments);
        if (success && report != null)
        {
            await _auditService.LogStatusChangeAsync("Report", id, null, "Submitted", "Approved", userId, User.Identity?.Name,
                ApprovalComments ?? "Finalized by head after 3-day deadline");
            await _notificationService.NotifyReportStatusChangedAsync(id, report.Title, report.AuthorId, "Approved (Finalized by Head)");
        }
        TempData[success ? "SuccessMessage" : "ErrorMessage"] =
            success ? "Report finalized and approved by head." : "Unable to finalize report.";
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
        IsHead = await _reportService.IsUserHeadOfCommitteeAsync(userId, Report.CommitteeId);
        CanEdit = IsAuthor && (Report.Status == ReportStatus.Draft || Report.Status == ReportStatus.FeedbackRequested);
        CanSubmit = IsAuthor && (Report.Status == ReportStatus.Draft || Report.Status == ReportStatus.FeedbackRequested);
        CanReview = await _reportService.CanUserReviewReportAsync(userId, Report.CommitteeId, Report.AuthorId);
    }
}
