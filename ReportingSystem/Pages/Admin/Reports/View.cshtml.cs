using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Models;

namespace ReportingSystem.Pages.Admin.Reports;

public class ViewModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ViewModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Report Report { get; set; } = null!;
    public List<ReportField> Fields { get; set; } = new();
    public Dictionary<int, ReportFieldValue> FieldValues { get; set; } = new();
    public List<Attachment> Attachments { get; set; } = new();

    // Upward flow items (Phase 4)
    public List<SuggestedAction> SuggestedActions { get; set; } = new();
    public List<ResourceRequest> ResourceRequests { get; set; } = new();
    public List<SupportRequest> SupportRequests { get; set; } = new();

    [BindProperty]
    public string? ReviewComments { get; set; }

    [BindProperty]
    public string ReviewAction { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        return await LoadReportAsync(id);
    }

    public async Task<IActionResult> OnPostReviewAsync(int id)
    {
        var report = await _context.Reports.FindAsync(id);
        if (report == null) return NotFound();

        switch (ReviewAction)
        {
            case "approve":
                report.Status = ReportStatus.Approved;
                report.ReviewedAt = DateTime.UtcNow;
                report.ReviewComments = ReviewComments;
                report.IsLocked = true;
                TempData["SuccessMessage"] = "Report approved and locked.";
                break;

            case "reject":
                if (string.IsNullOrWhiteSpace(ReviewComments))
                {
                    ModelState.AddModelError("ReviewComments", "Comments are required when rejecting.");
                    return await LoadReportAsync(id);
                }
                report.Status = ReportStatus.Rejected;
                report.ReviewedAt = DateTime.UtcNow;
                report.ReviewComments = ReviewComments;
                TempData["SuccessMessage"] = "Report rejected.";
                break;

            case "amend":
                report.Status = ReportStatus.Amended;
                report.ReviewComments = ReviewComments;
                report.AmendmentCount++;
                report.IsLocked = false;
                TempData["SuccessMessage"] = "Report sent back for amendment.";
                break;

            case "review":
                report.Status = ReportStatus.UnderReview;
                // Set reviewer
                var reviewerEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (reviewerEmail != null)
                {
                    var reviewer = await _context.Users.FirstOrDefaultAsync(u => u.Email == reviewerEmail);
                    if (reviewer != null)
                    {
                        report.AssignedReviewerId = reviewer.Id;
                    }
                }
                TempData["SuccessMessage"] = "Report marked as under review.";
                break;
        }

        report.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return RedirectToPage("View", new { id });
    }

    private async Task<IActionResult> LoadReportAsync(int id)
    {
        var report = await _context.Reports
            .Include(r => r.ReportTemplate)
            .Include(r => r.ReportPeriod)
            .Include(r => r.SubmittedBy)
                .ThenInclude(u => u.OrganizationalUnit)
            .Include(r => r.AssignedReviewer)
            .Include(r => r.FieldValues)
            .Include(r => r.Attachments)
            .Include(r => r.SuggestedActions)
                .ThenInclude(a => a.ReviewedBy)
            .Include(r => r.ResourceRequests)
                .ThenInclude(r => r.ReviewedBy)
            .Include(r => r.SupportRequests)
                .ThenInclude(s => s.AssignedTo)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (report == null) return NotFound();

        Report = report;

        Fields = await _context.ReportFields
            .Where(f => f.ReportTemplateId == report.ReportTemplateId && f.IsActive)
            .OrderBy(f => f.SectionOrder)
            .ThenBy(f => f.FieldOrder)
            .ToListAsync();

        FieldValues = report.FieldValues.ToDictionary(fv => fv.ReportFieldId);
        Attachments = report.Attachments.ToList();

        // Load upward flow items
        SuggestedActions = report.SuggestedActions.OrderBy(a => a.CreatedAt).ToList();
        ResourceRequests = report.ResourceRequests.OrderBy(r => r.CreatedAt).ToList();
        SupportRequests = report.SupportRequests.OrderBy(s => s.CreatedAt).ToList();

        return Page();
    }
}
